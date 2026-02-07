namespace NoteVui.Application.DTOs.Admin;

/// <summary>
/// Request DTO for setting/updating user subscription by admin.
/// </summary>
public class SetUserSubscriptionRequest
{
    /// <summary>
    /// The plan type to set for the user.
    /// 0 = Free, 1 = PremiumMonthly, 2 = PremiumYearly
    /// </summary>
    public int PlanType { get; set; }

    /// <summary>
    /// Optional: Custom end date for the subscription.
    /// If not provided, will be calculated based on plan type.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional: Whether to enable auto-renew.
    /// Default is false.
    /// </summary>
    public bool IsAutoRenew { get; set; } = false;
}
