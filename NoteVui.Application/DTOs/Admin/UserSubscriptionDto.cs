namespace NoteVui.Application.DTOs.Admin;

/// <summary>
/// DTO for returning user subscription information.
/// </summary>
public class UserSubscriptionDto
{
    /// <summary>
    /// The subscription ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The plan type name (Free, PremiumMonthly, PremiumYearly).
    /// </summary>
    public string PlanType { get; set; } = string.Empty;

    /// <summary>
    /// The subscription status (Active, Cancelled, Expired).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the subscription started.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// When the subscription ends.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether auto-renew is enabled.
    /// </summary>
    public bool IsAutoRenew { get; set; }

    /// <summary>
    /// Whether the subscription is currently active.
    /// </summary>
    public bool IsActive { get; set; }
}
