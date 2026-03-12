namespace g19_sep490_ealds.Server.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string toName, string otpCode, string expirationMinutes);
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);
}
