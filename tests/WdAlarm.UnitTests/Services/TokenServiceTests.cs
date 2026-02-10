using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using WdAlarm.Core.Entities;
using WdAlarm.Infrastructure.Services;
using Xunit;

namespace WdAlarm.UnitTests.Services;

public class TokenServiceTests
{
    private readonly IConfiguration configuration;
    private readonly TokenService tokenService;

    public TokenServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "ThisIsAVerySecretKeyForJWTTokenGeneration_MustBeAtLeast32CharactersLong!" },
            { "JwtSettings:Issuer", "WdAlarm.Api" },
            { "JwtSettings:Audience", "WdAlarm.Client" },
            { "JwtSettings:AccessTokenExpirationHours", "25" },
            { "JwtSettings:RefreshTokenExpirationDays", "7" }
        };

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        tokenService = new TokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        // Act
        var token = tokenService.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify token structure
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        Assert.Equal("WdAlarm.Api", jwtToken.Issuer);
        Assert.Equal("WdAlarm.Client", jwtToken.Audiences.First());
        Assert.Contains(jwtToken.Claims, c => c.Type == "nameid" && c.Value == user.Id.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == user.Email);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = tokenService.GenerateRefreshToken();
        var token2 = tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var token = tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token);
        
        // Should be able to convert from Base64
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };
        var token = tokenService.GenerateAccessToken(user);

        // Act
        var principal = tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
        
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal(user.Id.ToString(), userIdClaim.Value);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = tokenService.GetPrincipalFromExpiredToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeRequiredClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        // Act
        var token = tokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // Check for required claims
        Assert.Contains(jwtToken.Claims, c => c.Type == "nameid");
        Assert.Contains(jwtToken.Claims, c => c.Type == "unique_name");
        Assert.Contains(jwtToken.Claims, c => c.Type == "email");
        Assert.Contains(jwtToken.Claims, c => c.Type == "sub");
        Assert.Contains(jwtToken.Claims, c => c.Type == "jti");
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveCorrectExpiration()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        // Act
        var token = tokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var expectedExpiration = DateTime.UtcNow.AddHours(25);
        var timeDifference = Math.Abs((jwtToken.ValidTo - expectedExpiration).TotalMinutes);
        
        // Should be within 1 minute of expected expiration
        Assert.True(timeDifference < 1, $"Token expiration differs by {timeDifference} minutes");
    }
}
