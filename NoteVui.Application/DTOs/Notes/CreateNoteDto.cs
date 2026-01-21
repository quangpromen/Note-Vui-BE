using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Notes;

public class CreateNoteDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ShortPreview { get; set; }

    public string? FullContent { get; set; }

    public bool IsPinned { get; set; }
}
