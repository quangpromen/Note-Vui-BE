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

        // Count total users
        var totalUsers = await _userManager.Users
            .AsNoTracking()
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
        // Base query for users
        var query = _userManager.Users.AsNoTracking();

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
}
