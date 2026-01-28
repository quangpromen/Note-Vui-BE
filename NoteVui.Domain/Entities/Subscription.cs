using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoteVui.Domain.Entities.Identity;

namespace NoteVui.Domain.Entities;

[Table("subscriptions")]
public class Subscription
{
    [Key]
    [Column("sub_id")]
    public int SubId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("plan_id")]
    public int PlanId { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("status")]
    public byte Status { get; set; } = 1;

    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;

    [ForeignKey(nameof(PlanId))]
    public virtual Plan Plan { get; set; } = null!;
}
