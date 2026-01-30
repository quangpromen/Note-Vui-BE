using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoteVui.Domain.Entities.Common;
using NoteVui.Domain.Entities.Identity;

namespace NoteVui.Domain.Entities.Membership;

/// <summary>
/// Represents a payment transaction in the membership system.
/// Tracks all payment attempts for subscriptions.
/// </summary>
[Table("payment_transactions")]
public class PaymentTransaction : BaseEntity
{
    /// <summary>
    /// Foreign key to AppUser.
    /// </summary>
    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    [Column("amount", TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (default VND).
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "VND";

    /// <summary>
    /// Unique transaction code from the payment provider.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("transaction_code")]
    public string TransactionCode { get; set; } = string.Empty;

    /// <summary>
    /// Payment provider name (e.g., "Momo", "Store", "VNPay").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the transaction.
    /// </summary>
    [Column("status")]
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Optional description or notes about the transaction.
    /// </summary>
    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation Property
    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;
}
