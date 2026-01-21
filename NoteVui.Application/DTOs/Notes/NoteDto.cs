namespace NoteVui.Application.DTOs.Notes;

/// <summary>
/// DTO for returning note data to the client.
/// </summary>
public class NoteDto
{
    public int NoteId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ShortPreview { get; set; }
    public string? FullContent { get; set; }
    public bool IsPinned { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
