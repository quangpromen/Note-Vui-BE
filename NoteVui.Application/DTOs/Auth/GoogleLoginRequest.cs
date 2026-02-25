using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Auth;

public class GoogleLoginRequest
{
    /// <summary>
    /// The Google ID Token received from Google Sign-In on the client side.
    /// </summary>
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
