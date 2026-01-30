using Microsoft.EntityFrameworkCore;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities.Membership;

namespace NoteVui.Application.Services;

/// <summary>
/// Service for checking VIP/Premium membership status.
/// </summary>
public class VipService : IVipService
{
    private readonly IApplicationDbContext _context;

    public VipService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<bool> IsVipAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var activeSubscription = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId 
                        && s.Status == SubscriptionStatus.Active 
                        && s.EndDate > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        return activeSubscription != null;
    }

    /// <inheritdoc />
    public bool IsVip(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var activeSubscription = _context.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId 
                        && s.Status == SubscriptionStatus.Active 
                        && s.EndDate > DateTime.UtcNow)
            .FirstOrDefault();

        return activeSubscription != null;
    }
}
