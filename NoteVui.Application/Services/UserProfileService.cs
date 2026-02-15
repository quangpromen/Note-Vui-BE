using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoteVui.Application.DTOs.User;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities.Identity;
using NoteVui.Domain.Entities.Membership;
using NoteVui.Domain.Enums;

namespace NoteVui.Application.Services;

/// <summary>
/// Service for retrieving user profile information.
/// Read-only service - does not modify the database.
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IVipService _vipService;

    public UserProfileService(
        IApplicationDbContext context,
        UserManager<AppUser> userManager,
        IVipService vipService)
    {
        _context = context;
        _userManager = userManager;
        _vipService = vipService;
    }

    /// <inheritdoc />
    public async Task<UserProfileResponse?> GetUserProfileAsync(string userId)
    {
        // 1. Get user info from Identity
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        // 2. Get subscription info
        var subscription = await _context.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var isVip = await _vipService.IsVipAsync(userId);

        // 3. Get notes count
        var totalNotesBackedUp = await _context.Notes
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId);

        var activeNotes = await _context.Notes
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsDeleted);

        // 4. Get AI usage stats
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Parse userId to Guid for AiUsageLog query
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return null;
        }

        var aiUsageQuery = _context.AiUsageLogs
            .AsNoTracking()
            .Where(log => log.UserId == userGuid);

        // Count by periods
        var usedToday = await aiUsageQuery
            .CountAsync(log => log.CreatedAt >= todayStart);

        var usedThisMonth = await aiUsageQuery
            .CountAsync(log => log.CreatedAt >= monthStart);

        var usedThisYear = await aiUsageQuery
            .CountAsync(log => log.CreatedAt >= yearStart);

        var totalUsed = await aiUsageQuery.CountAsync();

        // Today's usage by action type
        var todayByAction = await aiUsageQuery
            .Where(log => log.CreatedAt >= todayStart)
            .GroupBy(log => log.ActionType)
            .Select(g => new AiActionUsage
            {
                ActionType = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();

        // 5. Build response
        return new UserProfileResponse
        {
            UserId = userId,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,

            Subscription = BuildSubscriptionInfo(subscription, isVip),

            TotalNotesBackedUp = totalNotesBackedUp,
            ActiveNotes = activeNotes,

            AiUsage = new AiUsageStats
            {
                UsedToday = usedToday,
                UsedThisMonth = usedThisMonth,
                UsedThisYear = usedThisYear,
                TotalUsed = totalUsed,
                TodayByAction = todayByAction
            }
        };
    }

    /// <summary>
    /// Builds subscription info from the UserSubscription entity.
    /// </summary>
    private static UserSubscriptionInfo BuildSubscriptionInfo(UserSubscription? subscription, bool isVip)
    {
        if (subscription == null)
        {
            return new UserSubscriptionInfo
            {
                PlanName = "Free",
                PlanType = "Free",
                IsVip = false,
                Status = null,
                StartDate = null,
                EndDate = null,
                DaysRemaining = null,
                IsAutoRenew = false
            };
        }

        var planName = subscription.PlanType switch
        {
            Domain.Entities.Membership.PlanType.Free => "Free",
            Domain.Entities.Membership.PlanType.PremiumMonthly => "Premium (Tháng)",
            Domain.Entities.Membership.PlanType.PremiumYearly => "Premium (Năm)",
            _ => "Free"
        };

        int? daysRemaining = null;
        if (isVip && subscription.EndDate > DateTime.UtcNow)
        {
            daysRemaining = (int)(subscription.EndDate - DateTime.UtcNow).TotalDays;
        }

        return new UserSubscriptionInfo
        {
            PlanName = planName,
            PlanType = subscription.PlanType.ToString(),
            IsVip = isVip,
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            DaysRemaining = daysRemaining,
            IsAutoRenew = subscription.IsAutoRenew
        };
    }
    /// <inheritdoc />
    public async Task<UserProfileResponse?> UpdateProfileAsync(string userId, EditProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        // Update fields
        user.FullName = request.FullName;
        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        await _userManager.UpdateAsync(user);

        // Return updated full profile
        return await GetUserProfileAsync(userId);
    }
}
