using NoteVui.Application.DTOs.Ai;
using NoteVui.Domain.Enums;

namespace NoteVui.Application.Interfaces;

/// <summary>
/// Interface for AI service operations.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Summarizes the provided content using AI.
    /// </summary>
    /// <param name="content">The content to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI response with summarized content.</returns>
    Task<AiResponse> SummarizeAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fixes grammar and spelling errors in the provided content.
    /// </summary>
    /// <param name="content">The content to fix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI response with corrected content.</returns>
    Task<AiResponse> FixGrammarAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Translates the provided content to the target language.
    /// </summary>
    /// <param name="content">The content to translate.</param>
    /// <param name="targetLanguage">The target language code (e.g., "en", "vi").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI response with translated content.</returns>
    Task<AiResponse> TranslateAsync(string content, string targetLanguage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates ideas or suggestions based on the provided content.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI response with generated ideas.</returns>
    Task<AiResponse> GenerateIdeasAsync(string content, CancellationToken cancellationToken = default);
}
