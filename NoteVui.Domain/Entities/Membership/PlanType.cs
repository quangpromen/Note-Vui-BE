namespace NoteVui.Domain.Entities.Membership;

/// <summary>
/// Represents the type of subscription plan.
/// </summary>
public enum PlanType
{
    /// <summary>
    /// Free tier with basic features.
    /// </summary>
    Free = 0,

    /// <summary>
    /// Monthly premium subscription.
    /// </summary>
    PremiumMonthly = 1,

    /// <summary>
    /// Yearly premium subscription with discount.
    /// </summary>
    PremiumYearly = 2
}
