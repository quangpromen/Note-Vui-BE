using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Auth;

public class UpdateProfileRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    public string? AvatarUrl { get; set; }
}
