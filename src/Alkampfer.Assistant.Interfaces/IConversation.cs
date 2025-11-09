namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Represents a conversation that manages messages with different roles.
/// </summary>
public interface IConversation
{
    /// <summary>
    /// Adds a message to the conversation.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The content of the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddMessageAsync(MessageRole role, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages in the conversation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of conversation messages.</returns>
    Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(CancellationToken cancellationToken = default);
}
