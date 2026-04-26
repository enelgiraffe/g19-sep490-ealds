using g19_sep490_ealds.Server.DTOs.Auth;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task LogoutAsync(int userId);
    Task ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<string> VerifyOtpAsync(VerifyOtpRequestDto request);
    Task ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
}
