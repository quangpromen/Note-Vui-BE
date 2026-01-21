using Microsoft.AspNetCore.Identity;
using NoteVui.Domain.Entities;

namespace NoteVui.Domain.Entities.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Navigation Property
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
