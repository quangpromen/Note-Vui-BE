using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Auth;

public class VerifyOtpRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must contain only digits.")]
    public string Otp { get; set; } = string.Empty;
}
