using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Core;

/// <summary>
/// Basic in-memory implementation of a conversation.
/// </summary>
public class Conversation : IConversation
{
    private readonly List<ConversationMessage> _messages = new();
    private readonly Dictionary<string, object?> _context = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Statistics Statistics { get; } = new();

    /// <inheritdoc />
    public IDictionary<string, object?> Context => _context;

    /// <inheritdoc />
    public Task AddMessageAsync(ConversationRole role, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        lock (_lock)
        {
            _messages.Add(new ConversationMessage(role, content));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<ConversationMessage>>(_messages.ToList());
        }
    }
}
