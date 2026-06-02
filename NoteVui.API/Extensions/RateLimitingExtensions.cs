using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace NoteVui.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthLimiter = "AuthLimiter";
    public const string ApiLimiter = "ApiLimiter";

    private const string AnonymousFallback = "anonymous_fallback";

    public static IServiceCollection AddCustomRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteTooManyRequestsProblemDetailsAsync;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: GetClientIdentifier(context),
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 120,
                        TokensPerPeriod = 20,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(AuthLimiter, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetRealClientIp(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy(ApiLimiter, context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: GetClientIdentifier(context),
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 40,
                        TokensPerPeriod = 10,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }

    public static string GetRealClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstIp = forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstIp) && IPAddress.TryParse(firstIp, out var parsedIp))
            {
                return parsedIp.ToString();
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? AnonymousFallback;
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{GetRealClientIp(context)}";
    }

    private static async ValueTask WriteTooManyRequestsProblemDetailsAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var retryAfter = TimeSpan.FromSeconds(60);
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadataRetryAfter))
        {
            retryAfter = metadataRetryAfter;
        }

        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        var httpContext = context.HttpContext;

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers[HeaderNames.RetryAfter] =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Tần suất gửi yêu cầu quá nhanh. Vui lòng thử lại sau.",
            Type = "https://www.rfc-editor.org/rfc/rfc6585#section-4",
            Instance = httpContext.Request.Path
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }
}
