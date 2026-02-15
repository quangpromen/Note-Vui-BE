using NoteVui.Application.DTOs.User;

namespace NoteVui.Application.Services.Interfaces;

/// <summary>
/// Interface for user profile operations.
/// Provides access to user information, subscription, notes count, and AI usage.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Gets the complete profile information for a user.
    /// Includes personal info, subscription details, note counts, and AI usage stats.
    /// </summary>
    /// <param name="userId">The user's ID (string from Identity).</param>
    /// <returns>User profile response DTO or null if user not found.</returns>
    Task<UserProfileResponse?> GetUserProfileAsync(string userId);

    /// <summary>
    /// Updates the user's own profile (FullName, AvatarUrl).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="request">The edit profile request.</param>
    /// <returns>Updated profile response or null if user not found.</returns>
    Task<UserProfileResponse?> UpdateProfileAsync(string userId, EditProfileRequest request);
}
