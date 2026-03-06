using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoteVui.Domain.Entities.Common;
using NoteVui.Domain.Entities.Identity;
using NoteVui.Domain.Enums;

namespace NoteVui.Domain.Entities.Membership;

/// <summary>
/// Represents a user's request to upgrade their subscription plan manually (e.g., via bank transfer).
/// </summary>
[Table("subscription_requests")]
public class SubscriptionRequest : BaseEntity
{
    /// <summary>
    /// Foreign key to AppUser (the user who created the request).
    /// </summary>
    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The target plan type the user wants to upgrade to.
    /// </summary>
    [Column("plan_type")]
    public PlanType PlanType { get; set; }

    /// <summary>
    /// Current status of the request.
    /// </summary>
    [Column("status")]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    /// <summary>
    /// Optional note from the user (e.g., payment proof, transaction ID).
    /// </summary>
    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    /// <summary>
    /// Optional note from the admin (e.g., reason for rejection).
    /// </summary>
    [MaxLength(500)]
    [Column("admin_note")]
    public string? AdminNote { get; set; }

    /// <summary>
    /// Foreign key to AppUser (the admin who processed the request).
    /// </summary>
    [Column("processed_by_user_id")]
    public string? ProcessedByUserId { get; set; }

    /// <summary>
    /// When the request was processed (approved or rejected).
    /// </summary>
    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;

    [ForeignKey(nameof(ProcessedByUserId))]
    public virtual AppUser? ProcessedByUser { get; set; }
}
