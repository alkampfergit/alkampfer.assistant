using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Core;

/// <summary>
/// Provides ambient context for the current conversation using AsyncLocal storage.
/// This allows the conversation to flow through async/await calls without explicit passing.
/// </summary>
public static class ConversationContext
{
    private static readonly AsyncLocal<IConversation?> _current = new();

    /// <summary>
    /// Gets or sets the current conversation for the async context.
    /// </summary>
    public static IConversation? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets the current conversation, throwing an exception if not set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no conversation is set in the current context.</exception>
    public static IConversation CurrentRequired =>
        Current ?? throw new InvalidOperationException("No conversation is set in the current context.");

    /// <summary>
    /// Starts a conversation by setting it as the current conversation in the async context.
    /// </summary>
    /// <param name="conversation">The conversation to start.</param>
    /// <returns>An IDisposable that clears the conversation when disposed.</returns>
    public static IDisposable StartConversation(IConversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        
        Current = conversation;
        return new ConversationScope();
    }

    private sealed class ConversationScope : IDisposable
    {
        public void Dispose()
        {
            Current = null;
        }
    }
}
