using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteVui.Application.DTOs.Admin;
using SubscriptionDTOs = NoteVui.Application.DTOs.Subscription;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Enums;

namespace NoteVui.API.Controllers;

/// <summary>
/// Admin Portal API Controller.
/// Provides endpoints for system statistics and user management.
/// Restricted to Admin role only.
/// </summary>
[Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ISubscriptionRequestService _subscriptionRequestService;

    public AdminController(IAdminService adminService, ISubscriptionRequestService subscriptionRequestService)
    {
        _adminService = adminService;
        _subscriptionRequestService = subscriptionRequestService;
    }

    /// <summary>
    /// Gets dashboard statistics including revenue, user counts, and AI usage.
    /// </summary>
    /// <returns>Dashboard statistics.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminDashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminDashboardStatsDto>> GetStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Gets a paginated list of users.
    /// </summary>
    /// <param name="search">Optional search term to filter by email or name.</param>
    /// <param name="page">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 100).</param>
    /// <returns>Paginated list of user summaries.</returns>
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _adminService.GetUsersAsync(search, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Locks or unlocks a user account.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="request">Lock/unlock request body.</param>
    /// <returns>Success status.</returns>
    [HttpPost("users/{id}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockUser(string id, [FromBody] LockUserRequest request)
    {
        var success = await _adminService.LockUserAsync(id, request.Lock);

        if (!success)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var message = request.Lock
            ? "Đã khóa tài khoản người dùng thành công."
            : "Đã mở khóa tài khoản người dùng thành công.";

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Gets the subscription information for a specific user.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <returns>User subscription information.</returns>
    [HttpGet("users/{id}/subscription")]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSubscription(string id)
    {
        var result = await _adminService.GetUserSubscriptionAsync(id);

        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Sets or updates the subscription for a user (activate/modify VIP).
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="request">The subscription settings.</param>
    /// <returns>Updated subscription information.</returns>
    /// <remarks>
    /// PlanType values:
    /// - 0: Free
    /// - 1: PremiumMonthly
    /// - 2: PremiumYearly
    /// 
    /// If EndDate is not provided, it will be calculated based on PlanType:
    /// - Free: 100 years from now
    /// - PremiumMonthly: 1 month from now
    /// - PremiumYearly: 1 year from now
    /// </remarks>
    [HttpPut("users/{id}/subscription")]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserSubscription(string id, [FromBody] SetUserSubscriptionRequest request)
    {
        // Validate PlanType
        if (request.PlanType < 0 || request.PlanType > 2)
        {
            return BadRequest(new { message = "PlanType không hợp lệ. Giá trị hợp lệ: 0 (Free), 1 (PremiumMonthly), 2 (PremiumYearly)." });
        }

        var result = await _adminService.SetUserSubscriptionAsync(id, request);

        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var planName = request.PlanType switch
        {
            0 => "Free",
            1 => "Premium (Tháng)",
            2 => "Premium (Năm)",
            _ => "Unknown"
        };

        return Ok(new
        {
            success = true,
            message = $"Đã cập nhật gói {planName} cho người dùng thành công.",
            data = result
        });
    }

    /// <summary>
    /// Gets detailed information for a specific user.
    /// Includes: user info, subscription, notes stats, AI usage, and account status.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <returns>Detailed user information.</returns>
    [HttpGet("users/{id}/detail")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetail(string id)
    {
        var result = await _adminService.GetUserDetailAsync(id);

        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Edits a user's profile information (FullName, Email, AvatarUrl).
    /// Admin can change user's email - something users cannot do themselves.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="request">The profile edit data.</param>
    /// <returns>Updated detailed user information.</returns>
    [HttpPut("users/{id}/profile")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditUserProfile(string id, [FromBody] AdminEditUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _adminService.EditUserProfileAsync(id, request);

            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            return Ok(new
            {
                success = true,
                message = "Đã cập nhật thông tin người dùng thành công.",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new user by Admin. If the email already exists, 
    /// returns the existing user details without creating a duplicate.
    /// </summary>
    /// <param name="request">The create user request data.</param>
    /// <returns>The created or existing detailed user information.</returns>
    [HttpPost("users")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _adminService.CreateUserAsync(request);

            return Ok(new
            {
                success = true,
                message = "Xử lý người dùng thành công.",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // SUBSCRIPTION REQUEST MANAGEMENT (Admin)
    // ==========================================

    /// <summary>
    /// Gets a paginated list of subscription upgrade requests.
    /// Supports filtering by status and searching by user email/name.
    /// </summary>
    /// <param name="status">Optional filter: 0=Pending, 1=Approved, 2=Rejected, 3=Cancelled.</param>
    /// <param name="search">Optional search term (user email or name).</param>
    /// <param name="page">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 100).</param>
    [HttpGet("subscription-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSubscriptionRequests(
        [FromQuery] RequestStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _subscriptionRequestService.GetAdminRequestsAsync(status, search, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Approves a pending subscription upgrade request.
    /// This will activate the user's VIP subscription automatically.
    /// </summary>
    /// <param name="id">The request ID to approve.</param>
    [HttpPost("subscription-requests/{id}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveSubscriptionRequest(int id)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { message = "Admin not authenticated" });

        try
        {
            var result = await _subscriptionRequestService.ApproveRequestAsync(id, adminUserId);
            return Ok(new
            {
                success = true,
                message = $"Đã phê duyệt yêu cầu nâng cấp gói {result.PlanName} cho {result.UserFullName} thành công.",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rejects a pending subscription upgrade request with an optional reason.
    /// </summary>
    /// <param name="id">The request ID to reject.</param>
    /// <param name="request">Optional rejection reason.</param>
    [HttpPost("subscription-requests/{id}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectSubscriptionRequest(int id, [FromBody] SubscriptionDTOs.RejectSubscriptionRequestDto? request)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { message = "Admin not authenticated" });

        try
        {
            var result = await _subscriptionRequestService.RejectRequestAsync(id, adminUserId, request?.Reason);
            return Ok(new
            {
                success = true,
                message = $"Đã từ chối yêu cầu nâng cấp của {result.UserFullName}.",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
