namespace NoteVui.Application.DTOs.Ai;

/// <summary>
/// Response DTO for AI operations.
/// </summary>
public class AiResponse
{
    /// <summary>
    /// The result of the AI operation.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of input tokens consumed.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Remaining daily quota after this request.
    /// </summary>
    public int RemainingQuota { get; set; }

    /// <summary>
    /// Creates a successful AI response.
    /// </summary>
    public static AiResponse Success(string result, int inputTokens = 0, int outputTokens = 0, int remainingQuota = 0)
    {
        return new AiResponse
        {
            Result = result,
            IsSuccess = true,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            RemainingQuota = remainingQuota
        };
    }

    /// <summary>
    /// Creates a failed AI response.
    /// </summary>
    public static AiResponse Failure(string errorMessage)
    {
        return new AiResponse
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
