namespace NoteVui.Domain.Enums;

/// <summary>
/// Defines the types of AI actions available in the application.
/// </summary>
public enum AiActionType
{
    /// <summary>
    /// Summarize note content using AI.
    /// </summary>
    Summarize = 0,

    /// <summary>
    /// Fix grammar and spelling errors in note content.
    /// </summary>
    FixGrammar = 1,

    /// <summary>
    /// Translate note content to another language.
    /// </summary>
    Translate = 2,

    /// <summary>
    /// Generate ideas or suggestions based on note content.
    /// </summary>
    GenerateIdeas = 3
}
