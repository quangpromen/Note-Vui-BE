using NoteVui.Application.DTOs.Common;
using NoteVui.Application.DTOs.Subscription;
using NoteVui.Domain.Enums;

namespace NoteVui.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing subscription upgrade requests.
/// Handles both User-side (create/view) and Admin-side (list/approve/reject) operations.
/// </summary>
public interface ISubscriptionRequestService
{
    // ==========================================
    // USER OPERATIONS
    // ==========================================

    /// <summary>
    /// Creates a new subscription upgrade request for the current user.
    /// Validates that the user doesn't have a pending request already.
    /// </summary>
    /// <param name="userId">The requesting user's ID.</param>
    /// <param name="request">The request details (target plan, note).</param>
    /// <returns>The created request details.</returns>
    Task<SubscriptionRequestResponseDto> CreateRequestAsync(string userId, CreateSubscriptionRequestDto request);

    /// <summary>
    /// Gets all subscription requests for the current user, ordered by newest first.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>List of the user's subscription requests.</returns>
    Task<List<SubscriptionRequestResponseDto>> GetUserRequestsAsync(string userId);

    /// <summary>
    /// Cancels a pending subscription request (only if status is Pending).
    /// </summary>
    /// <param name="userId">The user's ID (for ownership validation).</param>
    /// <param name="requestId">The request ID to cancel.</param>
    /// <returns>True if cancelled successfully.</returns>
    Task<bool> CancelRequestAsync(string userId, int requestId);

    // ==========================================
    // ADMIN OPERATIONS
    // ==========================================

    /// <summary>
    /// Gets a paginated list of subscription requests for Admin review.
    /// Supports filtering by status and searching by user email/name.
    /// </summary>
    /// <param name="status">Optional filter by request status.</param>
    /// <param name="search">Optional search term (user email or name).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated list of subscription requests with user info.</returns>
    Task<PagedResultDto<AdminSubscriptionRequestDto>> GetAdminRequestsAsync(
        RequestStatus? status, string? search, int page, int pageSize);

    /// <summary>
    /// Approves a pending subscription request.
    /// Updates the user's subscription (UserSubscription) within a transaction.
    /// </summary>
    /// <param name="requestId">The request ID to approve.</param>
    /// <param name="adminUserId">The admin user's ID (for audit trail).</param>
    /// <returns>The updated request details.</returns>
    Task<AdminSubscriptionRequestDto> ApproveRequestAsync(int requestId, string adminUserId);

    /// <summary>
    /// Rejects a pending subscription request with an optional reason.
    /// </summary>
    /// <param name="requestId">The request ID to reject.</param>
    /// <param name="adminUserId">The admin user's ID (for audit trail).</param>
    /// <param name="reason">Optional reason for rejection (shown to the user).</param>
    /// <returns>The updated request details.</returns>
    Task<AdminSubscriptionRequestDto> RejectRequestAsync(int requestId, string adminUserId, string? reason);
}
