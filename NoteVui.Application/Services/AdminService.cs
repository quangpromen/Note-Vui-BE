using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoteVui.Application.DTOs.Admin;
using NoteVui.Application.DTOs.Common;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities.Identity;
using NoteVui.Domain.Entities.Membership;

namespace NoteVui.Application.Services;

/// <summary>
/// Service implementation for Admin Portal operations.
/// Handles statistics calculation and user management.
/// </summary>
public class AdminService : IAdminService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AdminService(IApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
    {
        // Calculate total revenue from successful transactions
        var totalRevenue = await _context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.Status == TransactionStatus.Success)
            .SumAsync(t => t.Amount);

        // Get list of Admin user IDs to exclude from stats
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var adminUserIds = adminUsers.Select(u => u.Id).ToHashSet();

        // Count total users (excluding Admin role)
        var totalUsers = await _userManager.Users
            .AsNoTracking()
            .Where(u => !adminUserIds.Contains(u.Id))
            .CountAsync();

        // Count active premium users (Premium subscription that is still valid)
        var activePremiumUsers = await _context.UserSubscriptions
            .AsNoTracking()
            .CountAsync(s => s.EndDate > DateTime.UtcNow
                && s.Status == SubscriptionStatus.Active
                && s.PlanType != PlanType.Free);

        // Count total AI requests
        var totalAiRequests = await _context.AiUsageLogs
            .AsNoTracking()
            .CountAsync();

        return new AdminDashboardStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalUsers = totalUsers,
            ActivePremiumUsers = activePremiumUsers,
            TotalAiRequests = totalAiRequests
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<UserSummaryDto>> GetUsersAsync(string? search, int page, int pageSize)
    {
        // Get list of Admin user IDs to exclude from results
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var adminUserIds = adminUsers.Select(u => u.Id).ToHashSet();

        // Base query for users, excluding Admin role users
        var query = _userManager.Users
            .AsNoTracking()
            .Where(u => !adminUserIds.Contains(u.Id));

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                u.FullName.ToLower().Contains(searchLower));
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Get paginated users with their subscriptions
        var users = await query
            .OrderByDescending(u => u.Id) // Order by registration (assumes Id is sequential or sortable)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.LockoutEnd
            })
            .ToListAsync();

        // Get user IDs for subscription lookup
        var userIds = users.Select(u => u.Id).ToList();

        // Get subscriptions for these users
        var subscriptions = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .Select(s => new { s.UserId, s.PlanType, s.Status, s.EndDate })
            .ToListAsync();

        // Map to DTOs
        var now = DateTimeOffset.UtcNow;
        var items = users.Select(u =>
        {
            var subscription = subscriptions.FirstOrDefault(s => s.UserId == u.Id);
            var planName = GetPlanDisplayName(subscription?.PlanType, subscription?.Status, subscription?.EndDate);

            return new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                PlanName = planName,
                JoinDate = DateTime.UtcNow, // Note: AppUser doesn't have a CreatedAt field, using current time as placeholder
                IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd > now
            };
        }).ToList();

        return new PagedResultDto<UserSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<bool> LockUserAsync(string userId, bool isLock)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        IdentityResult result;
        if (isLock)
        {
            // Lock the user indefinitely by setting lockout end to max value
            result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            // Unlock the user by setting lockout end to null (removes lockout)
            result = await _userManager.SetLockoutEndDateAsync(user, null);
        }

        return result.Succeeded;
    }

    /// <summary>
    /// Gets a display-friendly plan name based on subscription details.
    /// </summary>
    private static string GetPlanDisplayName(PlanType? planType, SubscriptionStatus? status, DateTime? endDate)
    {
        if (planType == null)
        {
            return "Free";
        }

        var planName = planType.Value switch
        {
            PlanType.Free => "Free",
            PlanType.PremiumMonthly => "Premium (Month)",
            PlanType.PremiumYearly => "Premium (Year)",
            _ => "Free"
        };

        // Check if subscription is still valid
        if (planType != PlanType.Free)
        {
            if (status != SubscriptionStatus.Active || (endDate.HasValue && endDate.Value <= DateTime.UtcNow))
            {
                planName += " (Expired)";
            }
        }

        return planName;
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto?> GetUserSubscriptionAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var subscription = await _context.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var now = DateTime.UtcNow;

        return new UserSubscriptionDto
        {
            Id = subscription?.Id ?? 0,
            UserId = userId,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PlanType = subscription?.PlanType.ToString() ?? "Free",
            Status = subscription?.Status.ToString() ?? "Active",
            StartDate = subscription?.StartDate ?? now,
            EndDate = subscription?.EndDate ?? now,
            IsAutoRenew = subscription?.IsAutoRenew ?? false,
            IsActive = subscription != null &&
                       subscription.Status == SubscriptionStatus.Active &&
                       subscription.EndDate > now &&
                       subscription.PlanType != PlanType.Free
        };
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto?> SetUserSubscriptionAsync(string userId, SetUserSubscriptionRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        // Validate plan type
        if (!Enum.IsDefined(typeof(PlanType), request.PlanType))
        {
            return null;
        }

        var planType = (PlanType)request.PlanType;
        var now = DateTime.UtcNow;

        // Calculate default end date based on plan type if not provided
        var endDate = request.EndDate ?? planType switch
        {
            PlanType.Free => now.AddYears(100), // Free plan doesn't expire
            PlanType.PremiumMonthly => now.AddMonths(1),
            PlanType.PremiumYearly => now.AddYears(1),
            _ => now.AddMonths(1)
        };

        // Get existing subscription or create new one
        var subscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (subscription == null)
        {
            // Create new subscription
            subscription = new UserSubscription
            {
                UserId = userId,
                PlanType = planType,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                EndDate = endDate,
                IsAutoRenew = request.IsAutoRenew
            };
            _context.UserSubscriptions.Add(subscription);
        }
        else
        {
            // Update existing subscription
            subscription.PlanType = planType;
            subscription.Status = SubscriptionStatus.Active;
            subscription.StartDate = now;
            subscription.EndDate = endDate;
            subscription.IsAutoRenew = request.IsAutoRenew;
        }

        await _context.SaveChangesAsync();

        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            UserId = userId,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PlanType = subscription.PlanType.ToString(),
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsAutoRenew = subscription.IsAutoRenew,
            IsActive = subscription.Status == SubscriptionStatus.Active &&
                       subscription.EndDate > now &&
                       subscription.PlanType != PlanType.Free
        };
    }
}
