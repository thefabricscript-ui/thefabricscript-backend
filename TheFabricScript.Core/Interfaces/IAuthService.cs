using TheFabricScript.Core.DTOs.Auth;

namespace TheFabricScript.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> GoogleLoginAsync(string googleToken);
    Task SendOtpAsync(string phone);
    Task<AuthResponseDto> VerifyOtpAsync(string phone, string otp);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordDto request);
    Task RevokeTokenAsync(Guid userId);
}
