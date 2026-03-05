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

    public AuthController(EaldsDbContext context, ITokenService tokenService, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
        _emailService = emailService;
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

        // Always return OK to avoid leaking whether the email exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user != null && user.Status != 0)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var expirationMinutes = int.Parse(
                _configuration["App:ResetPasswordTokenExpirationMinutes"] ?? "15");

            user.ResetPasswordToken = token;
            user.ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(expirationMinutes);
            await _context.SaveChangesAsync();

            var frontendBaseUrl = _configuration["App:FrontendBaseUrl"]?.TrimEnd('/');
            var resetLink = $"{frontendBaseUrl}/reset-password?token={token}";

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.UserId);
            var displayName = employee?.Name ?? user.Email;

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, displayName, resetLink);
            }
            catch (Exception ex)
            {
                // Log but do not expose the error to the caller
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthController>>();
                logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        }

        return Ok(new { message = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu." });
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
