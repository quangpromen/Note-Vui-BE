using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.User;

/// <summary>
/// Request DTO for user to edit their own profile.
/// </summary>
public class EditProfileRequest
{
    /// <summary>
    /// User's full name. Required.
    /// </summary>
    [Required(ErrorMessage = "Tên không được để trống")]
    [MaxLength(100, ErrorMessage = "Tên tối đa 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's avatar URL. Nullable - send null to keep current avatar.
    /// </summary>
    public string? AvatarUrl { get; set; }
}
