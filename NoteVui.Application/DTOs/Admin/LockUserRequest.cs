namespace NoteVui.Application.DTOs.Admin;

/// <summary>
/// Request DTO for locking/unlocking a user account.
/// </summary>
public class LockUserRequest
{
    /// <summary>
    /// True to lock the user, false to unlock.
    /// </summary>
    public bool Lock { get; set; }
}
