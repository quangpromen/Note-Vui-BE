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
}
