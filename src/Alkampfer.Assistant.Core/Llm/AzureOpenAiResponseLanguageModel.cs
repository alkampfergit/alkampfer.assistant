using System.ClientModel;
using System.Text;
using Alkampfer.Assistant.Interfaces.Llm;
using Azure.AI.OpenAI;
using OpenAI.Responses;

namespace Alkampfer.Assistant.Core.Llm;

#pragma warning disable OPENAI001

/// <summary>
/// Azure OpenAI language model implementation using the new Response API with support for reasoning.
/// This is a stateless implementation - each call is independent with no conversation history.
/// </summary>
public class AzureOpenAiResponseLanguageModel : ILanguageModel
{
    private readonly OpenAIResponseClient _responseClient;
    private readonly ResponseReasoningEffortLevel _reasoningEffortLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiResponseLanguageModel"/> class.
    /// </summary>
    /// <param name="azureEndpoint">The Azure OpenAI endpoint URL.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="deploymentId">The deployment ID (model name) to use.</param>
    /// <param name="reasoningEffortLevel">The reasoning effort level to use. If null, uses Low by default.</param>
    public AzureOpenAiResponseLanguageModel(
        string azureEndpoint,
        string apiKey,
        string deploymentId,
        ResponseReasoningEffortLevel? reasoningEffortLevel = null)
    {
        var clientOptions = new AzureOpenAIClientOptions(
            AzureOpenAIClientOptions.ServiceVersion.V2025_04_01_Preview);

        var client = new AzureOpenAIClient(
            new Uri(azureEndpoint),
            new ApiKeyCredential(apiKey),
            clientOptions);

        _responseClient = client.GetOpenAIResponseClient(deploymentId);
        _reasoningEffortLevel = reasoningEffortLevel ?? ResponseReasoningEffortLevel.Low;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiResponseLanguageModel"/> class with an existing response client.
    /// </summary>
    /// <param name="responseClient">The response client to use for completions.</param>
    /// <param name="reasoningEffortLevel">The reasoning effort level to use. If null, uses Low by default.</param>
    public AzureOpenAiResponseLanguageModel(
        OpenAIResponseClient responseClient,
        ResponseReasoningEffortLevel? reasoningEffortLevel = null)
    {
        _responseClient = responseClient ?? throw new ArgumentNullException(nameof(responseClient));
        _reasoningEffortLevel = reasoningEffortLevel ?? ResponseReasoningEffortLevel.Low;
    }

    /// <inheritdoc/>
    public async Task<LanguageModelResponse> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        var inputItems = new List<ResponseItem> { ResponseItem.CreateUserMessageItem(prompt) };

        var options = new ResponseCreationOptions
        {
            ReasoningOptions = new ResponseReasoningOptions()
            {
                ReasoningEffortLevel = _reasoningEffortLevel
            }
        };

        var result = await _responseClient.CreateResponseAsync(inputItems, options, cancellationToken);

        OpenAIResponse response = result;

        var responseText = new StringBuilder();

        foreach (var outputItem in response.OutputItems)
        {
            if (outputItem is ReasoningResponseItem reasoning)
            {
                // Include reasoning summary in the response
                var summaryText = reasoning.GetSummaryText();
                if (!string.IsNullOrWhiteSpace(summaryText))
                {
                    responseText.AppendLine($"[Reasoning]: {summaryText}");
                    responseText.AppendLine();
                }
            }
            else if (outputItem is MessageResponseItem message)
            {
                responseText.Append(message.Content[0].Text);
            }
        }

        // Extract token usage from the response
        var usage = response.Usage;
        var statistics = new LanguageModelStatistics(usage.InputTokenCount, usage.OutputTokenCount);

        return new LanguageModelResponse(responseText.ToString(), statistics, response);
    }
}

#pragma warning restore OPENAI001