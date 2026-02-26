using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Reset token is required.")]
    public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string NewPassword { get; set; } = string.Empty;
}
