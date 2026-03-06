using NoteVui.Domain.Entities.Membership;
using NoteVui.Domain.Enums;

namespace NoteVui.Application.DTOs.Subscription;

// ==========================================
// USER-FACING DTOs
// ==========================================

/// <summary>
/// Request DTO for creating a subscription upgrade request.
/// Sent by the User to Admin for manual approval.
/// </summary>
public class CreateSubscriptionRequestDto
{
    /// <summary>
    /// The target plan type the user wants to upgrade to.
    /// </summary>
    public PlanType PlanType { get; set; }

    /// <summary>
    /// Optional note from the user (e.g., bank transfer proof, transaction ID).
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Response DTO returned to the User showing their request status.
/// </summary>
public class SubscriptionRequestResponseDto
{
    public int Id { get; set; }

    /// <summary>
    /// The target plan type requested.
    /// </summary>
    public string PlanType { get; set; } = string.Empty;

    /// <summary>
    /// Friendly display name of the plan (e.g., "Premium Monthly").
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the request (Pending, Approved, Rejected, Cancelled).
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Note provided by the user.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Admin's note (e.g., reason for rejection).
    /// </summary>
    public string? AdminNote { get; set; }

    /// <summary>
    /// When the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the request was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

// ==========================================
// ADMIN-FACING DTOs
// ==========================================

/// <summary>
/// DTO shown to Admin with full request details including user info.
/// </summary>
public class AdminSubscriptionRequestDto
{
    public int Id { get; set; }

    // --- User Info ---
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    // --- Request Info ---
    public string PlanType { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? Note { get; set; }
    public string? AdminNote { get; set; }

    // --- Processing Info ---
    public string? ProcessedByUserName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for Admin to reject a subscription request.
/// </summary>
public class RejectSubscriptionRequestDto
{
    /// <summary>
    /// The reason for rejection (will be stored as AdminNote and shown to User).
    /// </summary>
    public string? Reason { get; set; }
}
