using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WdAlarm.Api.Models.Requests;
using WdAlarm.Api.Models.Responses;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Interfaces;

namespace WdAlarm.Api.Controllers;

/// <summary>
/// Authentication controller for user registration, login, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly ITokenService tokenService;
    private readonly IConfiguration configuration;
    private readonly ILogger<AuthController> logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.tokenService = tokenService;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>User information</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new
            {
                error = "ConflictError",
                message = "User with this email already exists"
            });
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false, // Can implement email confirmation later
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
            
            return BadRequest(new
            {
                error = "ValidationError",
                message = "User registration failed",
                details = result.Errors.Select(e => new { code = e.Code, description = e.Description })
            });
        }

        logger.LogInformation("User registered successfully: {UserId} ({Email})", user.Id, user.Email);

        return CreatedAtAction(
            nameof(Register),
            new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                Message = "Registration successful"
            });
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
            return Unauthorized(new
            {
                error = "AuthenticationError",
                message = "Invalid email or password"
            });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return Unauthorized(new
            {
                error = "AuthenticationError",
                message = "Invalid email or password"
            });
        }

        // Generate tokens
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        // Store refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(
            configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7));
        user.UpdatedAt = DateTimeOffset.UtcNow;
        
        await userManager.UpdateAsync(user);

        logger.LogInformation("User logged in successfully: {UserId} ({Email})", user.Id, user.Email);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = configuration.GetValue<int>("JwtSettings:AccessTokenExpirationHours", 25) * 3600,
            TokenType = "Bearer",
            UserId = user.Id,
            Email = user.Email!
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New JWT tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(Request.Headers.Authorization.ToString().Replace("Bearer ", ""));
        
        if (principal == null)
        {
            return Unauthorized(new
            {
                error = "AuthenticationError",
                message = "Invalid access token"
            });
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new
            {
                error = "AuthenticationError",
                message = "Invalid token claims"
            });
        }

        var user = await userManager.FindByIdAsync(userGuid.ToString());
        if (user == null || user.RefreshToken != request.RefreshToken)
        {
            logger.LogWarning("Invalid refresh token attempt for user: {UserId}", userId);
            return Unauthorized(new
            {
                error = "AuthenticationError",
                message = "Invalid refresh token"
            });
        }

        if (user.RefreshTokenExpiryTime <= DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Expired refresh token for user: {UserId}", userId);
            return Unauthorized(new
            {
                error = "AuthenticationError",
                message = "Refresh token expired"
            });
        }

        // Generate new tokens
        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        // Update refresh token
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(
            configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7));
        user.UpdatedAt = DateTimeOffset.UtcNow;
        
        await userManager.UpdateAsync(user);

        logger.LogInformation("Token refreshed for user: {UserId}", user.Id);

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = configuration.GetValue<int>("JwtSettings:AccessTokenExpirationHours", 25) * 3600,
            TokenType = "Bearer",
            UserId = user.Id,
            Email = user.Email!
        });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userGuid.ToString());
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            createdAt = user.CreatedAt
        });
    }
}
