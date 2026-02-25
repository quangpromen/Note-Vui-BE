using NoteVui.Application.DTOs.Auth;

namespace NoteVui.Application.Interfaces;

public interface IIdentityService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
    // Extra methods for features
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task RevokeTokenAsync(string userId);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);

    // OTP-based Registration Flow
    Task SendRegistrationOtpAsync(SendOtpRequest request);
    Task<string> VerifyRegistrationOtpAsync(VerifyOtpRequest request);
    Task<AuthResponse> CompleteRegistrationAsync(CompleteRegistrationRequest request);
}
