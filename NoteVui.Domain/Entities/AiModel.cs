using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteVui.Domain.Entities;

[Table("ai_models")]
public class AiModel
{
    [Key]
    [Column("model_id")]
    public int ModelId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("model_code")]
    public string ModelCode { get; set; } = string.Empty;

    [Column("cost_input", TypeName = "decimal(18, 10)")]
    public decimal CostInput { get; set; }

    [Column("cost_output", TypeName = "decimal(18, 10)")]
    public decimal CostOutput { get; set; }

    // Navigation
    [ForeignKey(nameof(ProviderId))]
    public virtual AiProvider Provider { get; set; } = null!;
}
