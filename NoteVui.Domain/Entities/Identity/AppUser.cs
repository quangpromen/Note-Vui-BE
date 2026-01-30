using Microsoft.AspNetCore.Identity;
using NoteVui.Domain.Entities;
using NoteVui.Domain.Entities.Membership;

namespace NoteVui.Domain.Entities.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Navigation Properties - Existing
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    
    // Navigation Properties - Membership System
    /// <summary>
    /// User's current subscription in the membership system (one-to-one).
    /// </summary>
    public virtual UserSubscription? UserSubscription { get; set; }
    
    /// <summary>
    /// User's payment transaction history (one-to-many).
    /// </summary>
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
