using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheFabricScript.Core.DTOs.Auth;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register with email + password</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    /// <summary>Login with email + password</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Google OAuth login</summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var result = await _authService.GoogleLoginAsync(request.IdToken);
        return Ok(result);
    }

    /// <summary>Send OTP to phone number</summary>
    [HttpPost("otp/send")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        await _authService.SendOtpAsync(request.Phone);
        return Ok(new { message = "OTP sent successfully" });
    }

    /// <summary>Verify OTP and get tokens</summary>
    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await _authService.VerifyOtpAsync(request.Phone, request.Otp);
        return Ok(result);
    }

    /// <summary>Refresh access token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(result);
    }

    /// <summary>Initiate forgot password flow</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "If an account exists, a reset link has been sent" });
    }

    /// <summary>Reset password with token</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var success = await _authService.ResetPasswordAsync(request);
        if (!success) return BadRequest(new { message = "Invalid or expired token" });
        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>Logout (revoke refresh token)</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        await _authService.RevokeTokenAsync(userId);
        return Ok(new { message = "Logged out successfully" });
    }
}

// ── Inline request DTOs for simple payloads ───────────────
public record GoogleLoginRequest(string IdToken);
public record SendOtpRequest(string Phone);
public record VerifyOtpRequest(string Phone, string Otp);
public record RefreshTokenRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
