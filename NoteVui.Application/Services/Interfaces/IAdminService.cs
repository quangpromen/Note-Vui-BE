using NoteVui.Application.DTOs.Admin;
using NoteVui.Application.DTOs.Common;

namespace NoteVui.Application.Services.Interfaces;

/// <summary>
/// Interface for Admin Portal operations.
/// Provides statistics and user management functionalities.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Gets dashboard statistics including revenue, user counts, and AI usage.
    /// </summary>
    /// <returns>Dashboard statistics DTO.</returns>
    Task<AdminDashboardStatsDto> GetDashboardStatsAsync();

    /// <summary>
    /// Gets a paginated list of users with their summary information.
    /// </summary>
    /// <param name="search">Optional search term to filter by email or name.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated list of user summaries.</returns>
    Task<PagedResultDto<UserSummaryDto>> GetUsersAsync(string? search, int page, int pageSize);

    /// <summary>
    /// Locks or unlocks a user account using Identity Lockout mechanism.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="isLock">True to lock the user, false to unlock.</param>
    /// <returns>True if the operation succeeded, false otherwise.</returns>
    Task<bool> LockUserAsync(string userId, bool isLock);

    /// <summary>
    /// Gets the subscription information for a specific user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>User subscription DTO or null if user not found.</returns>
    Task<UserSubscriptionDto?> GetUserSubscriptionAsync(string userId);

    /// <summary>
    /// Sets or creates a subscription for a user (activate VIP).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="request">The subscription settings.</param>
    /// <returns>The updated subscription DTO or null if user not found.</returns>
    Task<UserSubscriptionDto?> SetUserSubscriptionAsync(string userId, SetUserSubscriptionRequest request);

    /// <summary>
    /// Gets detailed information for a specific user including all stats.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>Detailed user information or null if user not found.</returns>
    Task<AdminUserDetailDto?> GetUserDetailAsync(string userId);

    /// <summary>
    /// Edits a user's profile information (FullName, Email, AvatarUrl).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="request">The edit request.</param>
    /// <returns>Updated user detail or null if user not found.</returns>
    Task<AdminUserDetailDto?> EditUserProfileAsync(string userId, AdminEditUserRequest request);

    /// <summary>
    /// Creates a new user. If a user with the same email already exists, 
    /// returns the existing user details seamlessly without creating a duplicate.
    /// </summary>
    /// <param name="request">The create user request data.</param>
    /// <returns>The created or existing user details.</returns>
    Task<AdminUserDetailDto> CreateUserAsync(AdminCreateUserRequest request);
}
