using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteVui.Application.DTOs.Ai;
using NoteVui.Application.Interfaces;
using NoteVui.Domain.Entities;
using NoteVui.Domain.Enums;

namespace NoteVui.API.Controllers;

/// <summary>
/// Controller for AI-powered note operations.
/// All endpoints require authentication and are subject to daily quota limits.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiController> _logger;

    private const int DEFAULT_DAILY_LIMIT = 20;

    public AiController(
        IAiService aiService,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IConfiguration configuration,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Summarizes the provided content using AI.
    /// </summary>
    /// <param name="request">The content to summarize.</param>
    /// <returns>Summarized content.</returns>
    [HttpPost("summarize")]
    [ProducesResponseType(typeof(AiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Summarize([FromBody] AiRequest request)
    {
        return await ProcessAiRequestAsync(request, AiActionType.Summarize, 
            () => _aiService.SummarizeAsync(request.Content));
    }

    /// <summary>
    /// Fixes grammar and spelling errors in the provided content.
    /// </summary>
    /// <param name="request">The content to fix.</param>
    /// <returns>Corrected content.</returns>
    [HttpPost("grammar")]
    [ProducesResponseType(typeof(AiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FixGrammar([FromBody] AiRequest request)
    {
        return await ProcessAiRequestAsync(request, AiActionType.FixGrammar, 
            () => _aiService.FixGrammarAsync(request.Content));
    }

    /// <summary>
    /// Translates the provided content to the target language.
    /// </summary>
    /// <param name="request">The content to translate with target language.</param>
    /// <returns>Translated content.</returns>
    [HttpPost("translate")]
    [ProducesResponseType(typeof(AiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Translate([FromBody] AiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetLanguage))
        {
            return BadRequest(new { error = "Target language is required for translation." });
        }

        return await ProcessAiRequestAsync(request, AiActionType.Translate, 
            () => _aiService.TranslateAsync(request.Content, request.TargetLanguage));
    }

    /// <summary>
    /// Generates ideas and suggestions based on the provided content.
    /// </summary>
    /// <param name="request">The content to analyze.</param>
    /// <returns>Generated ideas.</returns>
    [HttpPost("ideas")]
    [ProducesResponseType(typeof(AiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateIdeas([FromBody] AiRequest request)
    {
        return await ProcessAiRequestAsync(request, AiActionType.GenerateIdeas, 
            () => _aiService.GenerateIdeasAsync(request.Content));
    }

    /// <summary>
    /// Gets the current user's remaining AI quota for today.
    /// </summary>
    /// <returns>Quota information.</returns>
    [HttpGet("quota")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetQuota()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var dailyLimit = GetDailyLimit();
        var usedToday = await GetTodayUsageCountAsync(userId.Value);
        var remaining = Math.Max(0, dailyLimit - usedToday);

        return Ok(new 
        { 
            dailyLimit = dailyLimit,
            usedToday = usedToday,
            remaining = remaining,
            resetTime = DateTime.UtcNow.Date.AddDays(1).ToString("O")
        });
    }

    /// <summary>
    /// Processes an AI request with quota checking and logging.
    /// </summary>
    private async Task<IActionResult> ProcessAiRequestAsync(
        AiRequest request, 
        AiActionType actionType, 
        Func<Task<AiResponse>> aiOperation)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        // Check daily quota BEFORE making the AI call
        var dailyLimit = GetDailyLimit();
        var usedToday = await GetTodayUsageCountAsync(userId.Value);
        
        if (usedToday >= dailyLimit)
        {
            _logger.LogWarning("User {UserId} exceeded daily AI quota ({Limit})", userId, dailyLimit);
            return StatusCode(StatusCodes.Status402PaymentRequired, new AiResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Daily AI quota exceeded. You have used {usedToday}/{dailyLimit} requests today. Quota resets at midnight UTC.",
                RemainingQuota = 0
            });
        }

        // Create usage log entry (we log attempts, not just successes)
        var usageLog = new AiUsageLog
        {
            UserId = userId.Value,
            ActionType = actionType,
            NoteId = request.NoteId,
            Provider = "Gemini",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Call the AI service
            var response = await aiOperation();

            // Update usage log with results
            usageLog.InputTokens = response.InputTokens;
            usageLog.OutputTokens = response.OutputTokens;
            usageLog.IsSuccess = response.IsSuccess;
            usageLog.ErrorMessage = response.ErrorMessage;

            // Calculate remaining quota
            response.RemainingQuota = Math.Max(0, dailyLimit - usedToday - 1);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("configuration"))
        {
            // AI service not configured properly
            _logger.LogError(ex, "AI service configuration error");
            usageLog.IsSuccess = false;
            usageLog.ErrorMessage = "Server configuration error";
            
            return StatusCode(StatusCodes.Status503ServiceUnavailable, AiResponse.Failure(
                "AI service is currently unavailable. Please try again later."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AI controller for user {UserId}", userId);
            usageLog.IsSuccess = false;
            usageLog.ErrorMessage = "Unexpected error";
            
            return StatusCode(StatusCodes.Status500InternalServerError, AiResponse.Failure(
                "An unexpected error occurred. Please try again later."));
        }
        finally
        {
            // Always save the usage log for tracking (even failed attempts)
            try
            {
                _dbContext.AiUsageLogs.Add(usageLog);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save AI usage log for user {UserId}", userId);
                // Don't throw - logging failure shouldn't break the API response
            }
        }
    }

    /// <summary>
    /// Gets the count of AI requests made by the user today (UTC).
    /// </summary>
    private async Task<int> GetTodayUsageCountAsync(Guid userId)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        return await _dbContext.AiUsageLogs
            .Where(log => log.UserId == userId 
                       && log.CreatedAt >= todayStart 
                       && log.CreatedAt < todayEnd)
            .CountAsync();
    }

    /// <summary>
    /// Gets the daily AI request limit from configuration.
    /// </summary>
    private int GetDailyLimit()
    {
        var limitConfig = _configuration["AiSettings:DailyQuotaLimit"];
        return int.TryParse(limitConfig, out var limit) ? limit : DEFAULT_DAILY_LIMIT;
    }

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdString = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdString))
        {
            return null;
        }

        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}
