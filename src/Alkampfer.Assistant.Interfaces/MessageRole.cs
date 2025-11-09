namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Represents the role of a message in a conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// System message that sets the context or behavior.
    /// </summary>
    System,

    /// <summary>
    /// Message from the AI assistant.
    /// </summary>
    Assistant,

    /// <summary>
    /// Message from the user.
    /// </summary>
    User
}
