namespace NoteVui.Application.DTOs.Admin;

/// <summary>
/// DTO for Admin Dashboard statistics overview.
/// </summary>
public class AdminDashboardStatsDto
{
    /// <summary>
    /// Total revenue from all completed payment transactions.
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Total number of registered users in the system.
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of users with active Premium subscriptions (not expired).
    /// </summary>
    public int ActivePremiumUsers { get; set; }

    /// <summary>
    /// Total number of AI requests made across all users.
    /// </summary>
    public int TotalAiRequests { get; set; }
}
