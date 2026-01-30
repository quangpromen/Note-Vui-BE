using NoteVui.Domain.Entities.Membership;

namespace NoteVui.Application.DTOs.Subscription;

/// <summary>
/// Response DTO for subscription status check.
/// </summary>
public class SubscriptionStatusResponse
{
    /// <summary>
    /// Whether the user has an active VIP subscription.
    /// </summary>
    public bool IsVip { get; set; }
    
    /// <summary>
    /// The current plan type (Free, PremiumMonthly, PremiumYearly).
    /// </summary>
    public string PlanType { get; set; } = "Free";
    
    /// <summary>
    /// Current subscription status (Active, Cancelled, Expired, or null if no subscription).
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// When the subscription started.
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// When the subscription ends/expires.
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Days remaining until subscription expires.
    /// </summary>
    public int? DaysRemaining { get; set; }
    
    /// <summary>
    /// Whether auto-renewal is enabled.
    /// </summary>
    public bool IsAutoRenew { get; set; }
}

/// <summary>
/// DTO for subscription details.
/// </summary>
public class UserSubscriptionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public PlanType PlanType { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAutoRenew { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
