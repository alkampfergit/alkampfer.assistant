using System.ClientModel;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Llm;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace Alkampfer.Assistant.Core.Llm;

/// <summary>
/// Azure OpenAI language model implementation using the traditional chat completion API.
/// This is a stateless implementation - each call is independent with no conversation history.
/// </summary>
public class AzureOpenAiChatLanguageModel : ILanguageModel
{
    private readonly ChatClient _chatClient;

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
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiChatLanguageModel"/> class with an existing chat client.
    /// </summary>
    /// <param name="chatClient">The chat client to use for completions.</param>
    public AzureOpenAiChatLanguageModel(ChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <inheritdoc/>
    public async Task<LanguageModelResponse> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(prompt)
        };

        var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

        var assistantMessage = completion.Value.Content[0].Text;

        // Extract token usage from the completion
        var usage = completion.Value.Usage;
        var statistics = new LanguageModelStatistics(usage.InputTokenCount, usage.OutputTokenCount);

        return new LanguageModelResponse(assistantMessage, statistics, completion.Value);
    }

    /// <inheritdoc/>
    public LlmCapabilities GetCapability()
    {
        return new LlmCapabilities
        {
            SupportConversation = false
        };
    }
}
