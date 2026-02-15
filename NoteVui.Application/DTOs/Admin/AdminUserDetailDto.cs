namespace NoteVui.Application.DTOs.Admin;

/// <summary>
/// Detailed user information for Admin viewing.
/// Contains all user info, subscription, notes stats, AI usage, and account status.
/// </summary>
public class AdminUserDetailDto
{
    // ===== User Info =====
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsLocked { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }

    // ===== Subscription Info =====
    public AdminUserSubscriptionInfo Subscription { get; set; } = new();

    // ===== Notes Stats =====
    /// <summary>Total notes (including deleted).</summary>
    public int TotalNotes { get; set; }
    /// <summary>Active notes (not deleted).</summary>
    public int ActiveNotes { get; set; }
    /// <summary>Deleted notes (in trash).</summary>
    public int DeletedNotes { get; set; }
    /// <summary>Pinned notes count.</summary>
    public int PinnedNotes { get; set; }

    // ===== AI Usage Stats =====
    public AdminAiUsageStats AiUsage { get; set; } = new();
}

/// <summary>
/// Subscription info viewed by Admin.
/// </summary>
public class AdminUserSubscriptionInfo
{
    public int? SubscriptionId { get; set; }
    public string PlanName { get; set; } = "Free";
    public string PlanType { get; set; } = "Free";
    public int PlanTypeValue { get; set; } = 0;
    public bool IsVip { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsAutoRenew { get; set; }
}

/// <summary>
/// AI usage stats for Admin detail view.
/// </summary>
public class AdminAiUsageStats
{
    public int UsedToday { get; set; }
    public int UsedThisMonth { get; set; }
    public int UsedThisYear { get; set; }
    public int TotalUsed { get; set; }
}
