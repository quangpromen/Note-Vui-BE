using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NoteVui.Application.DTOs.Auth;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities.Identity;

namespace NoteVui.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IOtpService _otpService;
    private readonly IMailService _mailService;

    public IdentityService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration,
        IOtpService otpService,
        IMailService mailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _otpService = otpService;
        _mailService = mailService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new Exception("Email already exists.");
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // For security, generic message
            throw new Exception("Invalid email or password.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
        {
            throw new Exception("Invalid email or password.");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(token);
        if (principal == null)
        {
            throw new Exception("Invalid token");
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) throw new Exception("Invalid token claims");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
        {
            throw new Exception("Invalid or expired refresh token");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        user.FullName = request.FullName;
        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        await _userManager.UpdateAsync(user);
    }

    public async Task RevokeTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _userManager.UpdateAsync(user);
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        // 1. Verify Google ID Token
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new List<string>
                {
                    _configuration["GoogleAuth:ClientId"] ?? throw new Exception("Google ClientId is not configured")
                }
            };

            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new Exception("Invalid Google token.");
        }

        // 2. Extract email from the verified token
        var email = payload.Email;
        if (string.IsNullOrEmpty(email))
        {
            throw new Exception("Google account does not have an email.");
        }

        // 3. Check if user exists in our system
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new Exception("This Google account is not registered in the system. Please register first.");
        }

        // 4. Check if account is locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new Exception("Account is locked.");
        }

        // 5. Update profile info from Google if needed
        var needsUpdate = false;
        if (!string.IsNullOrEmpty(payload.Name) && string.IsNullOrEmpty(user.FullName))
        {
            user.FullName = payload.Name;
            needsUpdate = true;
        }
        if (!string.IsNullOrEmpty(payload.Picture) && string.IsNullOrEmpty(user.AvatarUrl))
        {
            user.AvatarUrl = payload.Picture;
            needsUpdate = true;
        }
        if (needsUpdate)
        {
            await _userManager.UpdateAsync(user);
        }

        // 6. Generate JWT token and return
        return await GenerateAuthResponseAsync(user);
    }

    #region OTP-Based Registration Flow

    /// <summary>
    /// Step 1: Send OTP to user's email for registration verification.
    /// Security: Does NOT reveal whether the email already exists (prevents email enumeration).
    /// </summary>
    public async Task SendRegistrationOtpAsync(SendOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Check if email already registered — but DON'T tell the client 
        // (prevent email enumeration attack). Still send nothing.
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            // Silently return — client sees "OTP sent" but no email goes out.
            // This prevents attackers from discovering valid emails.
            return;
        }

        // Check rate limit
        if (_otpService.IsRateLimited(email))
        {
            throw new Exception("Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau 10 phút.");
        }

        // Generate OTP
        var otp = _otpService.GenerateOtp(email);
        if (otp == null)
        {
            throw new Exception("Không thể gửi mã OTP lúc này. Vui lòng thử lại sau.");
        }

        // Build beautiful HTML email
        var emailBody = BuildOtpEmailBody(otp);

        // Send email
        await _mailService.SendEmailAsync(
            email,
            "NoteVui - Mã xác nhận đăng ký tài khoản",
            emailBody);
    }

    /// <summary>
    /// Step 2: Verify OTP and return a short-lived registration token.
    /// The token encodes email + fullName so the client cannot tamper with them.
    /// </summary>
    public async Task<string> VerifyRegistrationOtpAsync(VerifyOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Double-check email is not already registered
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            throw new Exception("Email này đã được đăng ký.");
        }

        var result = _otpService.VerifyOtp(email, request.Otp);

        switch (result)
        {
            case OtpVerificationResult.Success:
                // Generate a short-lived registration token (10 minutes)
                var registrationToken = GenerateRegistrationToken(email);
                // Remove OTP after successful verification (one-time use)
                _otpService.RemoveOtp(email);
                return registrationToken;

            case OtpVerificationResult.InvalidOtp:
                throw new Exception("Mã OTP không chính xác.");

            case OtpVerificationResult.Expired:
                throw new Exception("Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

            case OtpVerificationResult.TooManyAttempts:
                throw new Exception("Bạn đã nhập sai quá nhiều lần. Vui lòng yêu cầu mã OTP mới.");

            case OtpVerificationResult.NotFound:
                throw new Exception("Không tìm thấy mã OTP. Vui lòng yêu cầu mã mới.");

            default:
                throw new Exception("Đã xảy ra lỗi. Vui lòng thử lại.");
        }
    }

    /// <summary>
    /// Step 3: Complete registration using the registration token and password.
    /// Validates the token, extracts email + fullName, creates the user account.
    /// </summary>
    public async Task<AuthResponse> CompleteRegistrationAsync(CompleteRegistrationRequest request)
    {
        // Validate and extract email from registration token
        var (email, _) = ValidateRegistrationToken(request.RegistrationToken);

        // Final check: email not already registered (race condition protection)
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            throw new Exception("Email này đã được đăng ký.");
        }

        // Create user
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new Exception($"Không thể tạo tài khoản: {errors}");
        }

        // Send welcome email (fire-and-forget, do not block registration)
        _ = Task.Run(async () =>
        {
            try
            {
                var welcomeBody = BuildWelcomeEmailBody(request.FullName);
                await _mailService.SendEmailAsync(
                    email,
                    "NoteVui - Chào mừng bạn đến với NoteVui! 🎉",
                    welcomeBody);
            }
            catch
            {
                // Silently ignore — welcome email failure should not affect registration
            }
        });

        // Return auth response (auto-login after registration)
        return await GenerateAuthResponseAsync(user);
    }

    #endregion

    #region OTP-Based Forgot Password Flow

    public async Task ForgotPasswordSendOtpAsync(SendOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            throw new Exception("Email chưa tồn tại trong hệ thống. Vui lòng đăng ký tài khoản.");
        }

        if (_otpService.IsRateLimited(email))
        {
            throw new Exception("Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau 10 phút.");
        }

        var otp = _otpService.GenerateOtp(email);
        if (otp == null)
        {
            throw new Exception("Không thể gửi mã OTP lúc này. Vui lòng thử lại sau.");
        }

        var emailBody = BuildForgotPasswordOtpEmailBody(otp);

        await _mailService.SendEmailAsync(
            email,
            "NoteVui - Mã xác nhận quên mật khẩu",
            emailBody);
    }

    public async Task<string> ForgotPasswordVerifyOtpAsync(VerifyOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            throw new Exception("Email không hợp lệ.");
        }

        var result = _otpService.VerifyOtp(email, request.Otp);

        switch (result)
        {
            case OtpVerificationResult.Success:
                var resetToken = GenerateForgotPasswordToken(email);
                _otpService.RemoveOtp(email);
                return resetToken;

            case OtpVerificationResult.InvalidOtp:
                throw new Exception("Mã OTP không chính xác.");

            case OtpVerificationResult.Expired:
                throw new Exception("Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

            case OtpVerificationResult.TooManyAttempts:
                throw new Exception("Bạn đã nhập sai quá nhiều lần. Vui lòng yêu cầu mã OTP mới.");

            case OtpVerificationResult.NotFound:
                throw new Exception("Không tìm thấy mã OTP. Vui lòng yêu cầu mã mới.");

            default:
                throw new Exception("Đã xảy ra lỗi. Vui lòng thử lại.");
        }
    }

    public async Task ForgotPasswordResetAsync(ResetPasswordRequest request)
    {
        var email = ValidateForgotPasswordToken(request.ResetToken);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new Exception("Không tìm thấy người dùng.");
        }

        var identityResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityResetToken, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Không thể đặt lại mật khẩu: {errors}");
        }

        // Send success email (fire-and-forget, do not block the response)
        _ = Task.Run(async () =>
        {
            try
            {
                var successBody = BuildPasswordResetSuccessEmailBody(user.FullName ?? "bạn");
                await _mailService.SendEmailAsync(
                    email,
                    "NoteVui - Đổi mật khẩu thành công ✅",
                    successBody);
            }
            catch
            {
                // Silently ignore email sending errors
            }
        });
    }

    #endregion

    #region Private Helpers

    private async Task<AuthResponse> GenerateAuthResponseAsync(AppUser user)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("FullName", user.FullName ?? string.Empty)
        };

        // Đọc roles của user từ database và thêm vào token
        // Không phải hardcode - lấy từ bảng AspNetUserRoles trong DB
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var secretKey = _configuration["JwtSettings:Key"];
        if (string.IsNullOrEmpty(secretKey)) throw new Exception("JWT Key is missing in configuration");

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var tokenDurationStr = _configuration["JwtSettings:DurationInMinutes"];
        if (!int.TryParse(tokenDurationStr, out int tokenDuration)) tokenDuration = 60;

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            expires: DateTime.Now.AddMinutes(tokenDuration),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        var refreshToken = Guid.NewGuid().ToString();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);

        await _userManager.UpdateAsync(user);

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            AvatarUrl = user.AvatarUrl
        };
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var secretKey = _configuration["JwtSettings:Key"];
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    /// <summary>
    /// Generates a short-lived JWT token (10 minutes) that encodes 
    /// the verified email and fullName for the final registration step.
    /// This prevents the client from changing the email after OTP verification.
    /// </summary>
    private string GenerateRegistrationToken(string email)
    {
        var secretKey = _configuration["JwtSettings:Key"];
        if (string.IsNullOrEmpty(secretKey)) throw new Exception("JWT Key is missing");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var claims = new[]
        {
            new Claim("purpose", "registration"),
            new Claim("email", email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates the registration token and extracts email + fullName.
    /// Throws if the token is invalid, expired, or has wrong purpose.
    /// </summary>
    private (string Email, string FullName) ValidateRegistrationToken(string token)
    {
        var secretKey = _configuration["JwtSettings:Key"];
        if (string.IsNullOrEmpty(secretKey)) throw new Exception("JWT Key is missing");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No tolerance for expiration
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("Token đăng ký không hợp lệ.");
            }

            // Verify this is actually a registration token, not a regular auth token
            var purpose = principal.FindFirstValue("purpose");
            if (purpose != "registration")
            {
                throw new Exception("Token không phải dùng cho đăng ký.");
            }

            var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email);
            // FullName was stored in the OTP send step, we pass it through the token
            // For simplicity, we'll get it from the registration flow
            var fullName = principal.FindFirstValue("fullName") ?? string.Empty;

            if (string.IsNullOrEmpty(email))
            {
                throw new Exception("Token đăng ký không chứa email.");
            }

            return (email, fullName);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new Exception("Token đăng ký đã hết hạn. Vui lòng thực hiện lại quy trình đăng ký.");
        }
        catch (SecurityTokenException)
        {
            throw new Exception("Token đăng ký không hợp lệ.");
        }
    }

    /// <summary>
    /// Builds a professional HTML email template for OTP verification.
    /// </summary>
    private static string BuildOtpEmailBody(string otp)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f4f7fa; font-family:Segoe UI, Arial, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 0;'>
        <tr>
            <td align='center'>
                <table width='480' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:16px; box-shadow:0 4px 24px rgba(0,0,0,0.08); overflow:hidden;'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #6366f1, #8b5cf6); padding:32px; text-align:center;'>
                            <h1 style='color:#ffffff; margin:0; font-size:28px; font-weight:700;'>📝 NoteVui</h1>
                            <p style='color:rgba(255,255,255,0.85); margin:8px 0 0; font-size:14px;'>Xác nhận đăng ký tài khoản</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding:32px;'>
                            <p style='color:#1e293b; font-size:16px; margin:0 0 16px;'>Xin chào,</p>
                            <p style='color:#475569; font-size:14px; line-height:1.6; margin:0 0 24px;'>
                                Cảm ơn bạn đã đăng ký tài khoản NoteVui. Vui lòng sử dụng mã OTP bên dưới để xác nhận email của bạn:
                            </p>
                            <!-- OTP Code -->
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' style='padding:20px 0;'>
                                        <div style='background:linear-gradient(135deg, #f0f0ff, #e8e0ff); border:2px dashed #6366f1; border-radius:12px; padding:20px 40px; display:inline-block;'>
                                            <span style='font-size:36px; font-weight:800; color:#4f46e5; letter-spacing:8px; font-family:monospace;'>{otp}</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            <!-- Warning -->
                            <div style='background:#fef3c7; border-left:4px solid #f59e0b; padding:12px 16px; border-radius:0 8px 8px 0; margin:20px 0;'>
                                <p style='color:#92400e; font-size:13px; margin:0;'>
                                    ⚠️ Mã OTP có hiệu lực trong <strong>5 phút</strong>. Không chia sẻ mã này với bất kỳ ai.
                                </p>
                            </div>
                            <p style='color:#94a3b8; font-size:13px; line-height:1.5; margin:16px 0 0;'>
                                Nếu bạn không yêu cầu đăng ký, vui lòng bỏ qua email này.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background:#f8fafc; padding:20px 32px; text-align:center; border-top:1px solid #e2e8f0;'>
                            <p style='color:#94a3b8; font-size:12px; margin:0;'>© 2026 NoteVui. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Builds a professional HTML welcome email after successful registration.
    /// </summary>
    private static string BuildWelcomeEmailBody(string fullName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f4f7fa; font-family:Segoe UI, Arial, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 0;'>
        <tr>
            <td align='center'>
                <table width='480' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:16px; box-shadow:0 4px 24px rgba(0,0,0,0.08); overflow:hidden;'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #10b981, #059669); padding:32px; text-align:center;'>
                            <h1 style='color:#ffffff; margin:0; font-size:28px; font-weight:700;'>🎉 Chào mừng!</h1>
                            <p style='color:rgba(255,255,255,0.85); margin:8px 0 0; font-size:14px;'>Tài khoản NoteVui của bạn đã sẵn sàng</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding:32px;'>
                            <p style='color:#1e293b; font-size:16px; margin:0 0 16px;'>Xin chào <strong>{fullName}</strong>,</p>
                            <p style='color:#475569; font-size:14px; line-height:1.6; margin:0 0 24px;'>
                                Chúc mừng bạn đã đăng ký tài khoản <strong>NoteVui</strong> thành công! 🎊
                            </p>
                            <p style='color:#475569; font-size:14px; line-height:1.6; margin:0 0 24px;'>
                                Bạn có thể bắt đầu sử dụng ứng dụng ngay bây giờ với các tính năng:
                            </p>
                            <!-- Features -->
                            <table width='100%' cellpadding='0' cellspacing='0' style='margin:0 0 24px;'>
                                <tr>
                                    <td style='padding:10px 0; border-bottom:1px solid #f1f5f9;'>
                                        <span style='font-size:18px; margin-right:8px;'>📝</span>
                                        <span style='color:#334155; font-size:14px;'>Tạo và quản lý ghi chú không giới hạn</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding:10px 0; border-bottom:1px solid #f1f5f9;'>
                                        <span style='font-size:18px; margin-right:8px;'>☁️</span>
                                        <span style='color:#334155; font-size:14px;'>Đồng bộ dữ liệu đám mây an toàn</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding:10px 0;'>
                                        <span style='font-size:18px; margin-right:8px;'>🤖</span>
                                        <span style='color:#334155; font-size:14px;'>Trợ lý AI thông minh (Nâng cấp Premium)</span>
                                    </td>
                                </tr>
                            </table>
                            <p style='color:#94a3b8; font-size:13px; line-height:1.5; margin:0;'>
                                Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ đội ngũ hỗ trợ của chúng tôi.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background:#f8fafc; padding:20px 32px; text-align:center; border-top:1px solid #e2e8f0;'>
                            <p style='color:#94a3b8; font-size:12px; margin:0;'>© 2026 NoteVui. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GenerateForgotPasswordToken(string email)
    {
        var secretKey = _configuration["JwtSettings:Key"];
        if (string.IsNullOrEmpty(secretKey)) throw new Exception("JWT Key is missing");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var claims = new[]
        {
            new Claim("purpose", "forgot_password"),
            new Claim("email", email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string ValidateForgotPasswordToken(string token)
    {
        var secretKey = _configuration["JwtSettings:Key"];
        if (string.IsNullOrEmpty(secretKey)) throw new Exception("JWT Key is missing");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("Token không hợp lệ.");
            }

            var purpose = principal.FindFirstValue("purpose");
            if (purpose != "forgot_password")
            {
                throw new Exception("Token không hợp lệ cho tác vụ này.");
            }

            var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                throw new Exception("Token không chứa email.");
            }

            return email;
        }
        catch (SecurityTokenExpiredException)
        {
            throw new Exception("Token đã hết hạn. Vui lòng thực hiện lại quy trình.");
        }
        catch (SecurityTokenException)
        {
            throw new Exception("Token không hợp lệ.");
        }
    }

    private static string BuildForgotPasswordOtpEmailBody(string otp)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f4f7fa; font-family:Segoe UI, Arial, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 0;'>
        <tr>
            <td align='center'>
                <table width='480' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:16px; box-shadow:0 4px 24px rgba(0,0,0,0.08); overflow:hidden;'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #f43f5e, #e11d48); padding:32px; text-align:center;'>
                            <h1 style='color:#ffffff; margin:0; font-size:28px; font-weight:700;'>📝 NoteVui</h1>
                            <p style='color:rgba(255,255,255,0.85); margin:8px 0 0; font-size:14px;'>Khôi phục mật khẩu</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding:32px;'>
                            <p style='color:#1e293b; font-size:16px; margin:0 0 16px;'>Xin chào,</p>
                            <p style='color:#475569; font-size:14px; line-height:1.6; margin:0 0 24px;'>
                                Bạn vừa yêu cầu khôi phục mật khẩu. Vui lòng sử dụng mã OTP bên dưới để xác nhận:
                            </p>
                            <!-- OTP Code -->
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' style='padding:20px 0;'>
                                        <div style='background:linear-gradient(135deg, #fff0f2, #ffe4e6); border:2px dashed #f43f5e; border-radius:12px; padding:20px 40px; display:inline-block;'>
                                            <span style='font-size:36px; font-weight:800; color:#e11d48; letter-spacing:8px; font-family:monospace;'>{otp}</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            <!-- Warning -->
                            <div style='background:#fef3c7; border-left:4px solid #f59e0b; padding:12px 16px; border-radius:0 8px 8px 0; margin:20px 0;'>
                                <p style='color:#92400e; font-size:13px; margin:0;'>
                                    ⚠️ Mã OTP có hiệu lực trong <strong>5 phút</strong>. Không chia sẻ mã này với bất kỳ ai.
                                </p>
                            </div>
                            <p style='color:#94a3b8; font-size:13px; line-height:1.5; margin:16px 0 0;'>
                                Nếu bạn không yêu cầu khôi phục mật khẩu, vui lòng bỏ qua email này hoặc liên hệ hỗ trợ.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background:#f8fafc; padding:20px 32px; text-align:center; border-top:1px solid #e2e8f0;'>
                            <p style='color:#94a3b8; font-size:12px; margin:0;'>© 2026 NoteVui. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string BuildPasswordResetSuccessEmailBody(string fullName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f4f7fa; font-family:Segoe UI, Arial, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 0;'>
        <tr>
            <td align='center'>
                <table width='480' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:16px; box-shadow:0 4px 24px rgba(0,0,0,0.08); overflow:hidden;'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #10b981, #059669); padding:32px; text-align:center;'>
                            <h1 style='color:#ffffff; margin:0; font-size:28px; font-weight:700;'>✅ Thành công!</h1>
                            <p style='color:rgba(255,255,255,0.85); margin:8px 0 0; font-size:14px;'>Mật khẩu của bạn đã được thay đổi</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding:32px;'>
                            <p style='color:#1e293b; font-size:16px; margin:0 0 16px;'>Xin chào <strong>{fullName}</strong>,</p>
                            <p style='color:#475569; font-size:14px; line-height:1.6; margin:0 0 24px;'>
                                Mật khẩu tài khoản NoteVui của bạn vừa được cập nhật thành công.
                            </p>
                            <!-- Warning -->
                            <div style='background:#fef3c7; border-left:4px solid #f59e0b; padding:12px 16px; border-radius:0 8px 8px 0; margin:20px 0;'>
                                <p style='color:#92400e; font-size:13px; margin:0;'>
                                    ⚠️ Nếu bạn KHÔNG thực hiện thay đổi này, vui lòng liên hệ ngay với bộ phận hỗ trợ của chúng tôi để bảo mật tài khoản.
                                </p>
                            </div>
                            <p style='color:#94a3b8; font-size:13px; line-height:1.5; margin:16px 0 0;'>
                                Cảm ơn bạn đã sử dụng NoteVui!
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background:#f8fafc; padding:20px 32px; text-align:center; border-top:1px solid #e2e8f0;'>
                            <p style='color:#94a3b8; font-size:12px; margin:0;'>© 2026 NoteVui. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    #endregion
}
