namespace Alkampfer.Assistant.Console;

/// <summary>
/// Information about the conversation to be displayed in the sticky header.
/// </summary>
public class ConversationDisplayInfo
{
    /// <summary>
    /// Gets or sets a value indicating whether a conversation is currently active.
    /// </summary>
    public bool IsConversationActive { get; set; }

    /// <summary>
    /// Gets or sets the total input tokens used in the conversation.
    /// </summary>
    public int TotalInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total output tokens used in the conversation.
    /// </summary>
    public int TotalOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the input tokens for the last call.
    /// </summary>
    public int LastCallInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the output tokens for the last call.
    /// </summary>
    public int LastCallOutputTokens { get; set; }

    /// <summary>
    /// Gets the total tokens (input + output).
    /// </summary>
    public int TotalTokens => TotalInputTokens + TotalOutputTokens;

    /// <summary>
    /// Gets the last call total tokens.
    /// </summary>
    public int LastCallTotalTokens => LastCallInputTokens + LastCallOutputTokens;
}
