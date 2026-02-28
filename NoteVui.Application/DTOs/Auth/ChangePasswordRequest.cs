using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Auth;

/// <summary>
/// Request model for changing the authenticated user's password.
/// Requires the current password for identity verification before applying the change.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// The user's current password for identity verification.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// The new password to set. Must be at least 6 characters.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password. Must match <see cref="NewPassword"/>.
    /// </summary>
    [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
