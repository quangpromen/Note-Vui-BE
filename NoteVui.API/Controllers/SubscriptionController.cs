using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteVui.Application.DTOs.Subscription;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities.Membership;

namespace NoteVui.API.Controllers;

/// <summary>
/// API Controller for subscription and VIP status management.
/// </summary>
[Route("api/subscription")]
[ApiController]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly IVipService _vipService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public SubscriptionController(
        IVipService vipService, 
        ICurrentUserService currentUserService,
        IApplicationDbContext context)
    {
        _vipService = vipService;
        _currentUserService = currentUserService;
        _context = context;
    }

    /// <summary>
    /// Check if the current user has an active VIP subscription.
    /// </summary>
    /// <returns>VIP status (true/false)</returns>
    [HttpGet("status")]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        var subscription = await _context.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var isVip = await _vipService.IsVipAsync(userId);
        
        var response = new SubscriptionStatusResponse
        {
            IsVip = isVip,
            PlanType = subscription?.PlanType.ToString() ?? "Free",
            Status = subscription?.Status.ToString(),
            StartDate = subscription?.StartDate,
            EndDate = subscription?.EndDate,
            DaysRemaining = subscription != null && subscription.EndDate > DateTime.UtcNow 
                ? (int)(subscription.EndDate - DateTime.UtcNow).TotalDays 
                : null,
            IsAutoRenew = subscription?.IsAutoRenew ?? false
        };

        return Ok(response);
    }

    /// <summary>
    /// Check if the current user is VIP (simplified endpoint).
    /// </summary>
    /// <returns>{ isVip: true/false }</returns>
    [HttpGet("is-vip")]
    public async Task<IActionResult> IsVip()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        var isVip = await _vipService.IsVipAsync(userId);
        
        return Ok(new { isVip });
    }

    /// <summary>
    /// Get detailed subscription information for the current user.
    /// </summary>
    [HttpGet("details")]
    public async Task<IActionResult> GetSubscriptionDetails()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        var subscription = await _context.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (subscription == null)
        {
            return Ok(new 
            { 
                hasSubscription = false,
                message = "No subscription found. User is on Free plan."
            });
        }

        var dto = new UserSubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanType = subscription.PlanType,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsAutoRenew = subscription.IsAutoRenew,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };

        return Ok(new 
        { 
            hasSubscription = true,
            subscription = dto
        });
    }

    /// <summary>
    /// [DEV/TEST ONLY] Create a test subscription for the current user.
    /// This endpoint is for development testing purposes.
    /// </summary>
    [HttpPost("test-activate")]
    public async Task<IActionResult> TestActivateSubscription([FromQuery] int durationDays = 30, [FromQuery] PlanType planType = PlanType.PremiumMonthly)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        // Check if user already has a subscription
        var existingSubscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existingSubscription != null)
        {
            // Update existing subscription
            existingSubscription.PlanType = planType;
            existingSubscription.Status = SubscriptionStatus.Active;
            existingSubscription.StartDate = DateTime.UtcNow;
            existingSubscription.EndDate = DateTime.UtcNow.AddDays(durationDays);
            existingSubscription.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new subscription
            var newSubscription = new UserSubscription
            {
                UserId = userId,
                PlanType = planType,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(durationDays),
                IsAutoRenew = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserSubscriptions.Add(newSubscription);
        }

        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = $"Test subscription activated for {durationDays} days",
            planType = planType.ToString(),
            expiresAt = DateTime.UtcNow.AddDays(durationDays)
        });
    }
}
