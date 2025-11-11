using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Llm;

namespace Alkampfer.Assistant.Core;

/// <summary>
/// Manages conversations with a language model using the ambient ConversationContext.
/// This class handles sending user messages and receiving responses while maintaining
/// conversation history through the current conversation in ConversationContext.
/// </summary>
public class ConversationAgent
{
    private readonly ILanguageModel _languageModel;
    private readonly bool _supportsConversation;
    private string? _lastResponseId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationAgent"/> class.
    /// </summary>
    /// <param name="languageModel">The language model to use for generating responses.</param>
    public ConversationAgent(ILanguageModel languageModel)
    {
        _languageModel = languageModel ?? throw new ArgumentNullException(nameof(languageModel));
        _supportsConversation = _languageModel.GetCapability().SupportConversation;
    }

    /// <summary>
    /// Gets the last response ID from the language model, if available.
    /// </summary>
    public string? LastResponseId => _lastResponseId;

    /// <summary>
    /// Sends a user message and gets a response from the language model.
    /// The conversation is maintained in the current ConversationContext.
    /// If the model supports conversation mode, it will use the previous response ID to continue the conversation
    /// without rebuilding the entire history.
    /// </summary>
    /// <param name="userMessage">The message from the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the language model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no conversation is set in the current context.</exception>
    /// <exception cref="ArgumentException">Thrown when the user message is null or empty.</exception>
    public async Task<string> SendMessageAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("User message cannot be null or empty.", nameof(userMessage));
        }

        // Get the current conversation from the context (throws if not set)
        var conversation = ConversationContext.CurrentRequired;

        // Add the user message to the conversation
        await conversation.AddMessageAsync(ConversationRole.User, userMessage, cancellationToken);

        LanguageModelResponse response;

        if (_supportsConversation)
        {
            // Use the request-based API with optional previous conversation ID
            var request = new LlmRequest
            {
                Messages = new List<ConversationMessage> { new ConversationMessage(ConversationRole.User, userMessage) },
                PreviousConversationId = _lastResponseId
            };

            response = await _languageModel.GenerateResponseAsync(request, cancellationToken);
            
            // Store the response ID for the next request
            _lastResponseId = response.ResponseId;
        }
        else
        {
            // Fall back to traditional prompt-based approach
            // Get all messages to build the prompt
            var messages = await conversation.GetMessagesAsync(cancellationToken);

            // Build a prompt from the conversation history
            var prompt = BuildPrompt(messages);

            // Get response from the language model
            response = await _languageModel.GenerateResponseAsync(prompt, cancellationToken);
        }

        // Add the assistant's response to the conversation
        await conversation.AddMessageAsync(ConversationRole.Assistant, response.Response, cancellationToken);

        // Update conversation statistics with token usage
        conversation.Statistics.UpdateStats(
            response.Statistics.InputTokens,
            response.Statistics.OutputTokens);

        return response.Response;
    }

    /// <summary>
    /// Builds a prompt from the conversation messages.
    /// </summary>
    /// <param name="messages">The conversation messages.</param>
    /// <returns>A formatted prompt string.</returns>
    private static string BuildPrompt(IReadOnlyList<ConversationMessage> messages)
    {
        if (messages.Count == 0)
        {
            return string.Empty;
        }

        var promptBuilder = new System.Text.StringBuilder();

        foreach (var message in messages)
        {
            var rolePrefix = message.Role switch
            {
                ConversationRole.System => "System: ",
                ConversationRole.User => "User: ",
                ConversationRole.Assistant => "Assistant: ",
                _ => ""
            };

            promptBuilder.AppendLine($"{rolePrefix}{message.Content}");
        }

        return promptBuilder.ToString().TrimEnd();
    }
}
