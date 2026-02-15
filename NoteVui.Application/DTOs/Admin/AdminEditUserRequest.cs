using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Admin;

/// <summary>
/// Request DTO for admin to edit a user's profile.
/// </summary>
public class AdminEditUserRequest
{
    /// <summary>
    /// User's full name. Required.
    /// </summary>
    [Required(ErrorMessage = "Tên không được để trống")]
    [MaxLength(100, ErrorMessage = "Tên tối đa 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's email. Required.
    /// </summary>
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's avatar URL. Nullable.
    /// </summary>
    public string? AvatarUrl { get; set; }
}
