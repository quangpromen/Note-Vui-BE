using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Auth;

public class SendOtpRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
}
