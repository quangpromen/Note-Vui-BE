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
    private readonly ISubscriptionRequestService _subscriptionRequestService;

    public SubscriptionController(
        IVipService vipService,
        ICurrentUserService currentUserService,
        IApplicationDbContext context,
        ISubscriptionRequestService subscriptionRequestService)
    {
        _vipService = vipService;
        _currentUserService = currentUserService;
        _context = context;
        _subscriptionRequestService = subscriptionRequestService;
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

    // ==========================================
    // SUBSCRIPTION REQUEST ENDPOINTS (User)
    // ==========================================

    /// <summary>
    /// Creates a new subscription upgrade request.
    /// User must not have a pending request already.
    /// </summary>
    /// <param name="request">Request details (PlanType, Note).</param>
    /// <returns>The created request details.</returns>
    /// <remarks>
    /// PlanType values:
    /// - 1: PremiumMonthly
    /// - 2: PremiumYearly
    /// </remarks>
    [HttpPost("requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateSubscriptionRequestDto request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var result = await _subscriptionRequestService.CreateRequestAsync(userId, request);
            return Ok(new
            {
                success = true,
                message = "Yêu cầu nâng cấp đã được gửi thành công. Vui lòng chờ Admin phê duyệt.",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets all subscription requests for the current user.
    /// </summary>
    /// <returns>List of subscription requests.</returns>
    [HttpGet("requests/my")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        var result = await _subscriptionRequestService.GetUserRequestsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Cancels a pending subscription request.
    /// Only requests with status "Pending" can be cancelled.
    /// </summary>
    /// <param name="id">The request ID to cancel.</param>
    [HttpPut("requests/{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            await _subscriptionRequestService.CancelRequestAsync(userId, id);
            return Ok(new
            {
                success = true,
                message = "Đã hủy yêu cầu nâng cấp thành công."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

