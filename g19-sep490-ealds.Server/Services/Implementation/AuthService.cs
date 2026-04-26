using System.Security.Cryptography;
using g19_sep490_ealds.Server.DTOs.Auth;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AuthService : IAuthService
{
    private readonly EaldsDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    private const int MaxFailedLoginAttemptsBeforeLockout = 5;
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(30);

    private int AccountantRoleId => _configuration.GetValue("App:AccountantRoleId", 3);

    public AuthService(
        EaldsDbContext context,
        ITokenService tokenService,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        var now = DateTime.UtcNow;
        if (user.LockoutEnd.HasValue && user.LockoutEnd <= now)
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            await _context.SaveChangesAsync();
        }

        if (user.Status == 0)
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa.");

        if (user.LockoutEnd.HasValue && user.LockoutEnd > now)
        {
            var remaining = user.LockoutEnd.Value - now;
            var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
            throw new UnauthorizedAccessException(
                $"Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau {minutes} phút.");
        }

        if (user.Password != request.Password)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedLoginAttemptsBeforeLockout)
            {
                user.LockoutEnd = now.Add(LoginLockoutDuration);
                user.AccessFailedCount = 0;
            }
            await _context.SaveChangesAsync();

            if (user.LockoutEnd.HasValue && user.LockoutEnd > now)
                throw new UnauthorizedAccessException("Đăng nhập sai quá 5 lần. Tài khoản bị khóa trong 30 phút.");

            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        var roles = await GetUserRolesAsync(user.UserId);
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.UserId);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        return new LoginResponseDto
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
    }

    public async Task LogoutAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || user.Status == 0)
            throw new InvalidOperationException("Email không tồn tại trong hệ thống.");

        var expirationMinutes = int.Parse(_configuration["App:OtpExpirationMinutes"] ?? "10");
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        user.ResetPasswordToken = otpCode;
        user.ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(expirationMinutes);
        await _context.SaveChangesAsync();

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.UserId);
        var displayName = employee?.Name ?? user.Email;

        try
        {
            await _emailService.SendOtpEmailAsync(user.Email, displayName, otpCode, expirationMinutes.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", user.Email);
            throw;
        }
    }

    public async Task<string> VerifyOtpAsync(VerifyOtpRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null
            || user.ResetPasswordToken == null
            || user.ResetPasswordTokenExpiryTime == null
            || user.ResetPasswordTokenExpiryTime < DateTime.UtcNow
            || user.ResetPasswordToken != request.OtpCode)
        {
            throw new InvalidOperationException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var resetTokenExpirationMinutes = int.Parse(
            _configuration["App:ResetPasswordTokenExpirationMinutes"] ?? "15");

        user.ResetPasswordToken = resetToken;
        user.ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(resetTokenExpirationMinutes);
        await _context.SaveChangesAsync();

        return resetToken;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ResetPasswordToken == request.Token);

        if (user == null || user.ResetPasswordTokenExpiryTime == null || user.ResetPasswordTokenExpiryTime < DateTime.UtcNow)
            throw new InvalidOperationException("Token không hợp lệ hoặc đã hết hạn.");

        if (string.Equals(user.Password, request.NewPassword, StringComparison.Ordinal))
            throw new InvalidOperationException("Mật khẩu mới không được trùng với mật khẩu cũ.");

        user.Password = request.NewPassword;
        user.ResetPasswordToken = null;
        user.ResetPasswordTokenExpiryTime = null;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");

        var roles = await GetUserRolesAsync(user.UserId);
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.UserId);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        return new LoginResponseDto
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
    }

    private async Task<List<string>> GetUserRolesAsync(int userId)
    {
        var acctId = AccountantRoleId;
        var roleRows = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => new { ur.RoleId, Code = ur.Role != null ? ur.Role.Code : null })
            .ToListAsync();
        return roleRows
            .Select(r => RoleCanonicalization.CanonicalizeRoleCode(r.RoleId, r.Code, acctId))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
