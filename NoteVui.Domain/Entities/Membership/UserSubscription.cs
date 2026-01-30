using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoteVui.Domain.Entities.Common;
using NoteVui.Domain.Entities.Identity;

namespace NoteVui.Domain.Entities.Membership;

/// <summary>
/// Represents a user's subscription in the membership system.
/// Separate from the existing Subscription entity to isolate membership logic.
/// </summary>
[Table("user_subscriptions")]
public class UserSubscription : BaseEntity
{
    /// <summary>
    /// Foreign key to AppUser.
    /// </summary>
    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The type of subscription plan.
    /// </summary>
    [Column("plan_type")]
    public PlanType PlanType { get; set; } = PlanType.Free;

    /// <summary>
    /// Current status of the subscription.
    /// </summary>
    [Column("status")]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// When the subscription started.
    /// </summary>
    [Column("start_date")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the subscription ends.
    /// </summary>
    [Column("end_date")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether to auto-renew when the subscription expires.
    /// </summary>
    [Column("is_auto_renew")]
    public bool IsAutoRenew { get; set; } = false;

    // Navigation Property
    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;
}
