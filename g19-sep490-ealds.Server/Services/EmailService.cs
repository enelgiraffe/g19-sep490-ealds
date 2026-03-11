using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace g19_sep490_ealds.Server.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string toName, string otpCode, string expirationMinutes)
    {
        var host = _configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host is not configured.");
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var useSsl = bool.Parse(_configuration["Smtp:UseSsl"] ?? "false");
        var username = _configuration["Smtp:Username"] ?? throw new InvalidOperationException("SMTP Username is not configured.");
        var password = (_configuration["Smtp:Password"] ?? throw new InvalidOperationException("SMTP Password is not configured.")).Replace(" ", "");
        var fromName = _configuration["Smtp:FromName"] ?? "EALDS System";
        var fromAddress = _configuration["Smtp:FromAddress"] ?? username;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Mã OTP đặt lại mật khẩu EALDS";

        message.Body = new TextPart("html")
        {
            Text = BuildOtpEmailBody(toName, otpCode, expirationMinutes)
        };

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("OTP email sent to {Email}", toEmail);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var host = _configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host is not configured.");
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var useSsl = bool.Parse(_configuration["Smtp:UseSsl"] ?? "false");
        var username = _configuration["Smtp:Username"] ?? throw new InvalidOperationException("SMTP Username is not configured.");
        // Gmail App Passwords are shown with spaces (e.g. "rwck sjze lvpo kkeh") but must be sent without them
        var password = (_configuration["Smtp:Password"] ?? throw new InvalidOperationException("SMTP Password is not configured.")).Replace(" ", "");
        var fromName = _configuration["Smtp:FromName"] ?? "EALDS System";
        var fromAddress = _configuration["Smtp:FromAddress"] ?? username;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Đặt lại mật khẩu EALDS";

        var expirationMinutes = _configuration["App:ResetPasswordTokenExpirationMinutes"] ?? "15";

        message.Body = new TextPart("html")
        {
            Text = BuildResetEmailBody(toName, resetLink, expirationMinutes)
        };

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }

    private static string BuildOtpEmailBody(string name, string otpCode, string expirationMinutes)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head><meta charset="UTF-8"></head>
            <body style="font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
              <div style="background-color: #f8f9fa; border-radius: 8px; padding: 32px;">
                <h2 style="color: #1a73e8; margin-top: 0;">Đặt lại mật khẩu</h2>
                <p>Xin chào <strong>{name}</strong>,</p>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn trong hệ thống <strong>EALDS</strong>.</p>
                <p>Mã OTP của bạn là:</p>
                <div style="text-align: center; margin: 32px 0;">
                  <span style="display: inline-block; background-color: #1a73e8; color: white; font-size: 32px; font-weight: bold; letter-spacing: 8px; padding: 16px 32px; border-radius: 8px;">{otpCode}</span>
                </div>
                <p>Mã này sẽ hết hạn sau <strong>{expirationMinutes} phút</strong>.</p>
                <p style="font-size: 13px; color: #666;">Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này. Tài khoản của bạn vẫn an toàn.</p>
                <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;">
                <p style="font-size: 12px; color: #999; text-align: center;">© 2026 EALDS System. Đây là email tự động, vui lòng không trả lời.</p>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildResetEmailBody(string name, string resetLink, string expirationMinutes)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head><meta charset="UTF-8"></head>
            <body style="font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
              <div style="background-color: #f8f9fa; border-radius: 8px; padding: 32px;">
                <h2 style="color: #1a73e8; margin-top: 0;">Đặt lại mật khẩu</h2>
                <p>Xin chào <strong>{name}</strong>,</p>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn trong hệ thống <strong>EALDS</strong>.</p>
                <p>Nhấn vào nút bên dưới để đặt lại mật khẩu. Liên kết này sẽ hết hạn sau <strong>{expirationMinutes} phút</strong>.</p>
                <div style="text-align: center; margin: 32px 0;">
                  <a href="{resetLink}" 
                     style="background-color: #1a73e8; color: white; padding: 14px 28px; border-radius: 6px; text-decoration: none; font-weight: bold; display: inline-block;">
                    Đặt lại mật khẩu
                  </a>
                </div>
                <p style="font-size: 13px; color: #666;">Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này. Tài khoản của bạn vẫn an toàn.</p>
                <p style="font-size: 13px; color: #666;">Hoặc sao chép đường dẫn sau vào trình duyệt:<br>
                  <a href="{resetLink}" style="color: #1a73e8; word-break: break-all;">{resetLink}</a>
                </p>
                <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;">
                <p style="font-size: 12px; color: #999; text-align: center;">© 2026 EALDS System. Đây là email tự động, vui lòng không trả lời.</p>
              </div>
            </body>
            </html>
            """;
    }
}
