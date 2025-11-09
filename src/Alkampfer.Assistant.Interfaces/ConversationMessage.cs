namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Represents a message in a conversation.
/// </summary>
public record ConversationMessage(MessageRole Role, string Content);
