using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteVui.Domain.Entities;

[Table("ai_providers")]
public class AiProvider
{
    [Key]
    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("provider_code")]
    public string ProviderCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("provider_name")]
    public string ProviderName { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<AiModel> AiModels { get; set; } = new List<AiModel>();
}
