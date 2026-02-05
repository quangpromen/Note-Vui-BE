namespace NoteVui.Application.DTOs.Admin;

/// <summary>
/// DTO for user summary information in admin user management.
/// </summary>
public class UserSummaryDto
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the user's current subscription plan.
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Date when the user joined/registered.
    /// </summary>
    public DateTime JoinDate { get; set; }

    /// <summary>
    /// Indicates whether the user account is currently locked.
    /// </summary>
    public bool IsLocked { get; set; }
}
