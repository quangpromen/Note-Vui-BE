using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Auth;

public class CompleteRegistrationRequest
{
    [Required(ErrorMessage = "Registration token is required.")]
    public string RegistrationToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;
}
