using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WdAlarm.Api.Controllers;
using WdAlarm.Api.Models.Requests;
using WdAlarm.Api.Models.Responses;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Interfaces;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace WdAlarm.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> mockSignInManager;
    private readonly Mock<ITokenService> mockTokenService;
    private readonly Mock<IConfiguration> mockConfiguration;
    private readonly Mock<ILogger<AuthController>> mockLogger;
    private readonly AuthController controller;

    public AuthControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            mockUserManager.Object,
            contextAccessorMock.Object,
            userPrincipalFactoryMock.Object,
            null, null, null, null);

        mockTokenService = new Mock<ITokenService>();
        mockConfiguration = new Mock<IConfiguration>();
        mockLogger = new Mock<ILogger<AuthController>>();

        // Setup configuration
        mockConfiguration.Setup(c => c["JwtSettings:AccessTokenExpirationHours"]).Returns("25");
        mockConfiguration.Setup(c => c["JwtSettings:RefreshTokenExpirationDays"]).Returns("7");
        mockConfiguration.Setup(c => c.GetSection("JwtSettings:AccessTokenExpirationHours").Value).Returns("25");
        mockConfiguration.Setup(c => c.GetSection("JwtSettings:RefreshTokenExpirationDays").Value).Returns("7");

        controller = new AuthController(
            mockUserManager.Object,
            mockSignInManager.Object,
            mockTokenService.Object,
            mockConfiguration.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        mockUserManager
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await controller.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<RegisterResponse>(createdResult.Value);
        Assert.Equal(request.Email, response.Email);
        Assert.Equal("Registration successful", response.Message);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            FirstName = "Test",
            LastName = "User",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        var existingUser = new ApplicationUser { Email = "existing@example.com" };
        mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await controller.Register(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        mockUserManager
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var errors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 8 characters" }
        };

        mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Success);

        mockTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("test-access-token");

        mockTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

        mockUserManager
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("test-access-token", response.AccessToken);
        Assert.Equal("test-refresh-token", response.RefreshToken);
        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(user.Email, response.Email);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Test@123"
        };

        mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Login_SuccessfulLogin_UpdatesUserRefreshToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Success);

        mockTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("test-access-token");

        mockTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

        mockUserManager
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await controller.Login(request);

        // Assert
        Assert.Equal("test-refresh-token", user.RefreshToken);
        Assert.NotNull(user.RefreshTokenExpiryTime);
        Assert.True(user.RefreshTokenExpiryTime > DateTimeOffset.UtcNow);
        mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }
}
