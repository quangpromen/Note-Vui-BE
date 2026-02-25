namespace NoteVui.Application.Interfaces;

/// <summary>
/// In-memory OTP storage service.
/// Stores OTP codes with expiration, rate limiting, and brute-force protection.
/// No database modifications required.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates and stores a 6-digit OTP for the given email.
    /// Returns null if rate limited.
    /// </summary>
    string? GenerateOtp(string email);

    /// <summary>
    /// Verifies the OTP code for the given email.
    /// Returns true if valid, false if invalid/expired/locked.
    /// </summary>
    OtpVerificationResult VerifyOtp(string email, string otp);

    /// <summary>
    /// Checks if the email is currently rate-limited for sending OTPs.
    /// </summary>
    bool IsRateLimited(string email);

    /// <summary>
    /// Removes the OTP entry after successful registration.
    /// </summary>
    void RemoveOtp(string email);
}

public enum OtpVerificationResult
{
    Success,
    InvalidOtp,
    Expired,
    TooManyAttempts,
    NotFound
}
