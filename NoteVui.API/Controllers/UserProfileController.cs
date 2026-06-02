using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoteVui.API.Extensions;
using NoteVui.Application.DTOs.User;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;

namespace NoteVui.API.Controllers;

/// <summary>
/// Controller for user profile operations.
/// Allows authenticated users to view and edit their profile information,
/// subscription details, note counts, and AI usage statistics.
/// </summary>
[Route("api/user")]
[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.ApiLimiter)]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly ICurrentUserService _currentUserService;

    public UserProfileController(
        IUserProfileService userProfileService,
        ICurrentUserService currentUserService)
    {
        _userProfileService = userProfileService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get the current authenticated user's profile information.
    /// Includes: user info, subscription/plan details, backed-up notes count,
    /// and AI usage statistics (today, this month, this year).
    /// </summary>
    /// <returns>Complete user profile information.</returns>
    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var profile = await _userProfileService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Update the current authenticated user's profile.
    /// User can change their FullName and AvatarUrl.
    /// Returns the updated full profile information.
    /// </summary>
    /// <param name="request">Profile update data.</param>
    /// <returns>Updated full profile information.</returns>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] EditProfileRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var profile = await _userProfileService.UpdateProfileAsync(userId, request);
        if (profile == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new
        {
            success = true,
            message = "Cập nhật thông tin thành công",
            data = profile
        });
    }
}
