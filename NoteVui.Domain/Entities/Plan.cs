using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteVui.Domain.Entities;

[Table("plans")]
public class Plan
{
    [Key]
    [Column("plan_id")]
    public int PlanId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("plan_code")]
    public string PlanCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("plan_name")]
    public string PlanName { get; set; } = string.Empty;

    [Column("max_notes")]
    public int MaxNotes { get; set; }

    [Column("daily_ai_limit")]
    public int DailyAiLimit { get; set; }

    [Column("price", TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
