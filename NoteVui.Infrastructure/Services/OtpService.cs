using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using NoteVui.Application.Interfaces;

namespace NoteVui.Infrastructure.Services;

/// <summary>
/// Thread-safe, in-memory OTP service with comprehensive security measures.
/// 
/// Security features:
/// - OTP is hashed (SHA256) before storage — never stored in plain text
/// - Rate limiting: max 5 OTP sends per email per 10-minute window
/// - Brute-force protection: max 5 failed verify attempts → entry locked
/// - OTP expires after 5 minutes
/// - Automatic cleanup of expired entries via periodic timer
/// - Cryptographically secure random OTP generation
/// </summary>
public class OtpService : IOtpService, IDisposable
{
    private readonly ConcurrentDictionary<string, OtpEntry> _otpStore = new();
    private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimitStore = new();
    private readonly Timer _cleanupTimer;

    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_VERIFY_ATTEMPTS = 5;
    private const int MAX_SEND_ATTEMPTS = 5;
    private const int RATE_LIMIT_WINDOW_MINUTES = 10;
    private const int CLEANUP_INTERVAL_MINUTES = 15;

    public OtpService()
    {
        // Periodic cleanup of expired entries to prevent memory leaks
        _cleanupTimer = new Timer(
            callback: _ => CleanupExpiredEntries(),
            state: null,
            dueTime: TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES),
            period: TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES));
    }

    public string? GenerateOtp(string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (IsRateLimited(normalizedEmail))
        {
            return null;
        }

        // Generate cryptographically secure 6-digit OTP
        var otp = GenerateSecureOtp();
        var hashedOtp = HashOtp(otp);

        var entry = new OtpEntry
        {
            HashedOtp = hashedOtp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        _otpStore.AddOrUpdate(normalizedEmail, entry, (_, _) => entry);

        // Track rate limiting
        TrackSendAttempt(normalizedEmail);

        return otp;
    }

    public OtpVerificationResult VerifyOtp(string email, string otp)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (!_otpStore.TryGetValue(normalizedEmail, out var entry))
        {
            return OtpVerificationResult.NotFound;
        }

        // Check if locked due to too many failed attempts
        if (entry.FailedAttempts >= MAX_VERIFY_ATTEMPTS)
        {
            return OtpVerificationResult.TooManyAttempts;
        }

        // Check if expired
        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            _otpStore.TryRemove(normalizedEmail, out _);
            return OtpVerificationResult.Expired;
        }

        // Constant-time comparison to prevent timing attacks
        var hashedInput = HashOtp(otp);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashedInput),
            Encoding.UTF8.GetBytes(entry.HashedOtp)))
        {
            entry.FailedAttempts++;
            return OtpVerificationResult.InvalidOtp;
        }

        return OtpVerificationResult.Success;
    }

    public bool IsRateLimited(string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (!_rateLimitStore.TryGetValue(normalizedEmail, out var rateLimitEntry))
        {
            return false;
        }

        // Reset if window has passed
        if (DateTime.UtcNow > rateLimitEntry.WindowExpiresAt)
        {
            _rateLimitStore.TryRemove(normalizedEmail, out _);
            return false;
        }

        return rateLimitEntry.SendCount >= MAX_SEND_ATTEMPTS;
    }

    public void RemoveOtp(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        _otpStore.TryRemove(normalizedEmail, out _);
    }

    #region Private Helpers

    private static string GenerateSecureOtp()
    {
        // Use cryptographically secure random number generator
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);

        // Convert to a 6-digit number (100000 - 999999)
        var value = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000;
        return value.ToString();
    }

    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private void TrackSendAttempt(string normalizedEmail)
    {
        _rateLimitStore.AddOrUpdate(
            normalizedEmail,
            new RateLimitEntry
            {
                SendCount = 1,
                WindowExpiresAt = DateTime.UtcNow.AddMinutes(RATE_LIMIT_WINDOW_MINUTES)
            },
            (_, existing) =>
            {
                if (DateTime.UtcNow > existing.WindowExpiresAt)
                {
                    // Reset window
                    return new RateLimitEntry
                    {
                        SendCount = 1,
                        WindowExpiresAt = DateTime.UtcNow.AddMinutes(RATE_LIMIT_WINDOW_MINUTES)
                    };
                }

                existing.SendCount++;
                return existing;
            });
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;

        foreach (var kvp in _otpStore)
        {
            if (now > kvp.Value.ExpiresAt)
            {
                _otpStore.TryRemove(kvp.Key, out _);
            }
        }

        foreach (var kvp in _rateLimitStore)
        {
            if (now > kvp.Value.WindowExpiresAt)
            {
                _rateLimitStore.TryRemove(kvp.Key, out _);
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Inner Classes

    private class OtpEntry
    {
        public string HashedOtp { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class RateLimitEntry
    {
        public int SendCount { get; set; }
        public DateTime WindowExpiresAt { get; set; }
    }

    #endregion
}
