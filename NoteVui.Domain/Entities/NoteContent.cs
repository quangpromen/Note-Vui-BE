using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteVui.Domain.Entities;

[Table("note_contents")]
public class NoteContent
{
    [Key]
    [ForeignKey("Note")]
    [Column("note_id")]
    public int NoteId { get; set; }

    [Column("full_content", TypeName = "nvarchar(max)")]
    public string? FullContent { get; set; }

    public virtual Note Note { get; set; } = null!;
}
