using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Ai;

/// <summary>
/// Request DTO for AI operations.
/// </summary>
public class AiRequest
{
    /// <summary>
    /// The content to process with AI.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Content cannot be empty.")]
    [MaxLength(50000, ErrorMessage = "Content cannot exceed 50,000 characters.")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional target language for translation (e.g., "en", "vi", "ja").
    /// </summary>
    public string? TargetLanguage { get; set; }

    /// <summary>
    /// Optional note ID to associate with this AI action for tracking.
    /// </summary>
    public Guid? NoteId { get; set; }
}
