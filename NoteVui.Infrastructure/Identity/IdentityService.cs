using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NoteVui.Application.DTOs.Auth;
using NoteVui.Application.Interfaces;
using NoteVui.Domain.Entities.Identity;

namespace NoteVui.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public IdentityService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
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
}
