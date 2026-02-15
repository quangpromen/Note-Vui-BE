namespace NoteVui.Application.DTOs.User;

/// <summary>
/// Response DTO for user profile information.
/// Contains personal info, subscription, note count, and AI usage statistics.
/// </summary>
public class UserProfileResponse
{
    // ===== User Info =====
    /// <summary>
    /// User's unique identifier.
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
    /// User's avatar URL (nullable).
    /// </summary>
    public string? AvatarUrl { get; set; }

    // ===== Subscription Info =====
    /// <summary>
    /// Current subscription/plan details.
    /// </summary>
    public UserSubscriptionInfo Subscription { get; set; } = new();

    // ===== Notes Stats =====
    /// <summary>
    /// Total number of notes backed up (synced to server).
    /// </summary>
    public int TotalNotesBackedUp { get; set; }

    /// <summary>
    /// Number of notes that are not deleted.
    /// </summary>
    public int ActiveNotes { get; set; }

    // ===== AI Usage Stats =====
    /// <summary>
    /// AI usage statistics broken down by day, month, and year.
    /// </summary>
    public AiUsageStats AiUsage { get; set; } = new();
}

/// <summary>
/// Subscription information for the user profile.
/// </summary>
public class UserSubscriptionInfo
{
    /// <summary>
    /// The display name of the plan (e.g., "Free", "Premium (Month)", "Premium (Year)").
    /// </summary>
    public string PlanName { get; set; } = "Free";

    /// <summary>
    /// The plan type enum value as string (Free, PremiumMonthly, PremiumYearly).
    /// </summary>
    public string PlanType { get; set; } = "Free";

    /// <summary>
    /// Whether the user is currently a VIP (active premium subscription).
    /// </summary>
    public bool IsVip { get; set; }

    /// <summary>
    /// Current subscription status (Active, Cancelled, Expired).
    /// Null if user has no subscription record.
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
    /// Number of days remaining until subscription expires.
    /// Null if no active subscription.
    /// </summary>
    public int? DaysRemaining { get; set; }

    /// <summary>
    /// Whether auto-renewal is enabled.
    /// </summary>
    public bool IsAutoRenew { get; set; }
}

/// <summary>
/// AI usage statistics for the user.
/// </summary>
public class AiUsageStats
{
    /// <summary>
    /// Number of AI requests used today.
    /// </summary>
    public int UsedToday { get; set; }

    /// <summary>
    /// Number of AI requests used this month.
    /// </summary>
    public int UsedThisMonth { get; set; }

    /// <summary>
    /// Number of AI requests used this year.
    /// </summary>
    public int UsedThisYear { get; set; }

    /// <summary>
    /// Total AI requests used all time.
    /// </summary>
    public int TotalUsed { get; set; }

    /// <summary>
    /// Breakdown of AI usage by action type for today.
    /// </summary>
    public List<AiActionUsage> TodayByAction { get; set; } = new();
}

/// <summary>
/// AI usage count for a specific action type.
/// </summary>
public class AiActionUsage
{
    /// <summary>
    /// The AI action type name (e.g., "Summarize", "FixGrammar", "Translate", "GenerateIdeas").
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this action was used.
    /// </summary>
    public int Count { get; set; }
}
