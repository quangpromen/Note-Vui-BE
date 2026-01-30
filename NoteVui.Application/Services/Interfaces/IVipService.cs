namespace NoteVui.Application.Services.Interfaces;

/// <summary>
/// Service interface for checking VIP/Premium membership status.
/// </summary>
public interface IVipService
{
    /// <summary>
    /// Checks if a user has an active VIP/Premium subscription.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user has an active subscription that hasn't expired.</returns>
    Task<bool> IsVipAsync(string userId);

    /// <summary>
    /// Synchronous version for simple checks.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user has an active subscription that hasn't expired.</returns>
    bool IsVip(string userId);
}
