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
}
