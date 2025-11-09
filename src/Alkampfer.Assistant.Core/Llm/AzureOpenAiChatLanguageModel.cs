using System.ClientModel;
using Alkampfer.Assistant.Interfaces.Llm;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace Alkampfer.Assistant.Core.Llm;

/// <summary>
/// Azure OpenAI language model implementation using the traditional chat completion API.
/// </summary>
public class AzureOpenAiChatLanguageModel : ILanguageModel
{
    private readonly ChatClient _chatClient;
    private readonly List<ChatMessage> _messages;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiChatLanguageModel"/> class.
    /// </summary>
    /// <param name="azureEndpoint">The Azure OpenAI endpoint URL.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="deploymentId">The deployment ID (model name) to use.</param>
    public AzureOpenAiChatLanguageModel(string azureEndpoint, string apiKey, string deploymentId)
    {
        var client = new AzureOpenAIClient(
            new Uri(azureEndpoint),
            new ApiKeyCredential(apiKey));

        _chatClient = client.GetChatClient(deploymentId);
        _messages = new List<ChatMessage>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiChatLanguageModel"/> class with an existing chat client.
    /// </summary>
    /// <param name="chatClient">The chat client to use for completions.</param>
    public AzureOpenAiChatLanguageModel(ChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _messages = new List<ChatMessage>();
    }

    /// <inheritdoc/>
    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        _messages.Add(ChatMessage.CreateUserMessage(prompt));

        var completion = await _chatClient.CompleteChatAsync(_messages, cancellationToken: cancellationToken);

        var assistantMessage = completion.Value.Content[0].Text;
        _messages.Add(ChatMessage.CreateAssistantMessage(assistantMessage));

        return assistantMessage;
    }

    /// <summary>
    /// Clears the conversation history.
    /// </summary>
    public void ClearHistory()
    {
        _messages.Clear();
    }

    /// <summary>
    /// Gets the current conversation history.
    /// </summary>
    public IReadOnlyList<ChatMessage> History => _messages.AsReadOnly();
}
