namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Represents a request to a language model containing conversation history and optional previous conversation ID.
/// </summary>
public class LlmRequest
{
    /// <summary>
    /// Gets or sets the previous conversation ID to continue from.
    /// Only supported by models with SupportConversation capability.
    /// </summary>
    public string? PreviousConversationId { get; set; }

    /// <summary>
    /// Gets or sets the list of conversation messages.
    /// </summary>
    public List<ConversationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmRequest"/> class.
    /// </summary>
    public LlmRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmRequest"/> class with messages.
    /// </summary>
    /// <param name="messages">The conversation messages.</param>
    public LlmRequest(List<ConversationMessage> messages)
    {
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmRequest"/> class with messages and previous conversation ID.
    /// </summary>
    /// <param name="messages">The conversation messages.</param>
    /// <param name="previousConversationId">The previous conversation ID.</param>
    public LlmRequest(List<ConversationMessage> messages, string? previousConversationId)
    {
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        PreviousConversationId = previousConversationId;
    }
}
