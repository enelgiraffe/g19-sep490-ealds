using System.Security.Claims;
using System.Security.Cryptography;
using g19_sep490_ealds.Server.DTOs.Auth;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly EaldsDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AuthController> _logger;

    public AuthController(EaldsDbContext context, ITokenService tokenService, IConfiguration configuration, IEmailService emailService, IWebHostEnvironment env, ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
        _emailService = emailService;
        _env = env;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || user.Password != request.Password)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        if (user.Status == 0)
            return Unauthorized(new { message = "Tài khoản đã bị vô hiệu hóa." });

        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == user.UserId);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenExpirationDays = int.Parse(
            _configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserInfoDto
            {
                Id = user.UserId.ToString(),
                Email = user.Email,
                Name = employee?.Name ?? user.Email,
                Role = roles.FirstOrDefault() ?? string.Empty
            }
        };

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized();

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đăng xuất thành công." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || user.Status == 0)
            return BadRequest(new { message = "Email không tồn tại trong hệ thống." });

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expirationMinutes = int.Parse(
            _configuration["App:OtpExpirationMinutes"] ?? "10");

        user.ResetPasswordToken = otpCode;
        user.ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(expirationMinutes);
        await _context.SaveChangesAsync();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == user.UserId);
        var displayName = employee?.Name ?? user.Email;

        try
        {
            await _emailService.SendOtpEmailAsync(user.Email, displayName, otpCode, expirationMinutes.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", user.Email);

            if (_env.IsDevelopment())
                return StatusCode(500, new { message = "Gửi email thất bại.", error = ex.Message, detail = ex.InnerException?.Message });

            return StatusCode(500, new { message = "Gửi email thất bại. Vui lòng thử lại sau." });
        }

        return Ok(new { message = "Mã OTP đã được gửi đến email của bạn." });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null
            || user.ResetPasswordToken == null
            || user.ResetPasswordTokenExpiryTime == null
            || user.ResetPasswordTokenExpiryTime < DateTime.UtcNow
            || user.ResetPasswordToken != request.OtpCode)
        {
            return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn." });
        }

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var resetTokenExpirationMinutes = int.Parse(
            _configuration["App:ResetPasswordTokenExpirationMinutes"] ?? "15");

        user.ResetPasswordToken = resetToken;
        user.ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(resetTokenExpirationMinutes);
        await _context.SaveChangesAsync();

        return Ok(new { token = resetToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ResetPasswordToken == request.Token);

        if (user == null || user.ResetPasswordTokenExpiryTime == null || user.ResetPasswordTokenExpiryTime < DateTime.UtcNow)
            return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });

        user.Password = request.NewPassword;
        user.ResetPasswordToken = null;
        user.ResetPasswordTokenExpiryTime = null;

        // Invalidate any active refresh tokens
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });

        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == user.UserId);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenExpirationDays = int.Parse(
            _configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            User = new UserInfoDto
            {
                Id = user.UserId.ToString(),
                Email = user.Email,
                Name = employee?.Name ?? user.Email,
                Role = roles.FirstOrDefault() ?? string.Empty
            }
        };

        return Ok(response);
    }
}
