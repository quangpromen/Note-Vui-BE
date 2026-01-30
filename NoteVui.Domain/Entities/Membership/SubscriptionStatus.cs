namespace NoteVui.Domain.Entities.Membership;

/// <summary>
/// Represents the status of a user subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is currently active.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Subscription was cancelled by the user.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired = 2
}
