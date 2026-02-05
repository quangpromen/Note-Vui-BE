using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteVui.Application.DTOs.Admin;
using NoteVui.Application.Services.Interfaces;

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

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
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
}
