using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteVui.Application.DTOs.Auth;
using NoteVui.Application.Interfaces;

namespace NoteVui.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IIdentityService identityService, ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _identityService.RegisterAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _identityService.LoginAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var result = await _identityService.GoogleLoginAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _identityService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Change the authenticated user's password.
    /// Requires the current password for verification.
    /// After success, all refresh tokens are revoked — the client must re-login.
    /// A notification email is sent to the user.
    /// </summary>
    /// <param name="request">Current password, new password, and confirmation.</param>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _identityService.ChangePasswordAsync(userId, request);
            return Ok(new
            {
                success = true,
                message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại với mật khẩu mới."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return Unauthorized();

            await _identityService.UpdateProfileAsync(userId, request);
            return Ok(new { message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return Unauthorized();

            await _identityService.RevokeTokenAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Step 1: Send a 6-digit OTP to the user's email for registration verification.
    /// </summary>
    [HttpPost("register/send-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendRegistrationOtp([FromBody] SendOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _identityService.SendRegistrationOtpAsync(request);
            return Ok(new
            {
                success = true,
                message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (bao gồm thư rác)."
            });
        }
        catch (Exception ex)
        {
            // Rate limit → 429
            if (ex.Message.Contains("quá nhiều"))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Step 2: Verify the OTP code. Returns a short-lived registration token on success.
    /// </summary>
    [HttpPost("register/verify-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyRegistrationOtp([FromBody] VerifyOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var registrationToken = await _identityService.VerifyRegistrationOtpAsync(request);
            return Ok(new
            {
                success = true,
                message = "Xác nhận OTP thành công.",
                registrationToken
            });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("quá nhiều"))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Step 3: Complete registration with registration token, password and full name.
    /// Returns auth tokens on success (auto-login).
    /// </summary>
    [HttpPost("register/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _identityService.CompleteRegistrationAsync(request);
            return Ok(new
            {
                success = true,
                message = "Đăng ký tài khoản thành công!",
                data = result
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Step 1: Send a 6-digit OTP to the user's email for forgot password.
    /// </summary>
    [HttpPost("forgot-password/send-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendForgotPasswordOtp([FromBody] SendOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _identityService.ForgotPasswordSendOtpAsync(request);
            return Ok(new
            {
                success = true,
                message = "Mã OTP đã được gửi. Vui lòng kiểm tra hộp thư (bao gồm thư rác)."
            });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("quá nhiều"))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Step 2: Verify the forgot password OTP code. Returns a reset password token on success.
    /// </summary>
    [HttpPost("forgot-password/verify-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var resetToken = await _identityService.ForgotPasswordVerifyOtpAsync(request);
            return Ok(new
            {
                success = true,
                message = "Xác nhận OTP thành công.",
                resetToken
            });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("quá nhiều"))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Step 3: Reset password using the reset token and new password.
    /// </summary>
    [HttpPost("forgot-password/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _identityService.ForgotPasswordResetAsync(request);
            return Ok(new
            {
                success = true,
                message = "Mật khẩu đã được thiết lập lại thành công. Vui lòng đăng nhập với mật khẩu mới."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
