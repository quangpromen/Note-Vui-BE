namespace NoteVui.Domain.Entities.Membership;

/// <summary>
/// Represents the status of a payment transaction.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Transaction completed successfully.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Transaction failed.
    /// </summary>
    Failed = 2
}
