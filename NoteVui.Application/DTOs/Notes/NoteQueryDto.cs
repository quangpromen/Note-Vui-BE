namespace NoteVui.Application.DTOs.Notes;

public class NoteQueryDto
{
    public string? Search { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
