using System.ClientModel;
using Alkampfer.Assistant.Interfaces.Llm;
using Azure.AI.OpenAI;
using Microsoft.ML.Tokenizers;
using OpenAI.Embeddings;

namespace Alkampfer.Assistant.Core.Llm;

/// <summary>
/// OpenAI embedding model implementation that supports both Azure OpenAI and standard OpenAI API.
/// Uses the OpenAI embeddings API to generate vector embeddings from text.
/// </summary>
public class OpenAiEmbeddingModel : IEmbeddingModel
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly Tokenizer _tokenizer;
    private readonly string _modelName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiEmbeddingModel"/> class for Azure OpenAI.
    /// </summary>
    /// <param name="azureEndpoint">The Azure OpenAI endpoint URL.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="deploymentId">The deployment ID (model name) to use.</param>
    public OpenAiEmbeddingModel(string azureEndpoint, string apiKey, string deploymentId)
    {
        ArgumentNullException.ThrowIfNull(azureEndpoint);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(deploymentId);

        var client = new AzureOpenAIClient(
            new Uri(azureEndpoint),
            new ApiKeyCredential(apiKey));

        _embeddingClient = client.GetEmbeddingClient(deploymentId);
        _modelName = deploymentId;
        _tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiEmbeddingModel"/> class for standard OpenAI API.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="modelName">The model name to use (e.g., "text-embedding-3-small", "text-embedding-3-large").</param>
    public OpenAiEmbeddingModel(string apiKey, string modelName)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(modelName);

        var openAiClient = new global::OpenAI.OpenAIClient(apiKey);
        _embeddingClient = openAiClient.GetEmbeddingClient(modelName);
        _modelName = modelName;
        _tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiEmbeddingModel"/> class with an existing embedding client.
    /// </summary>
    /// <param name="embeddingClient">The embedding client to use.</param>
    /// <param name="modelName">The model name for token counting.</param>
    public OpenAiEmbeddingModel(EmbeddingClient embeddingClient, string modelName)
    {
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        _tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
    }

    /// <inheritdoc/>
    public async Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        EmbeddingTextType textType = EmbeddingTextType.Neutral,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        var textList = texts.ToList();
        if (textList.Count == 0)
        {
            throw new ArgumentException("At least one text must be provided.", nameof(texts));
        }

        // Note: OpenAI text-embedding-3 models don't support the textType parameter
        // The textType parameter is ignored for now
        var response = await _embeddingClient.GenerateEmbeddingsAsync(textList, cancellationToken: cancellationToken);

        var embeddings = new List<ReadOnlyMemory<float>>();
        foreach (var item in response.Value)
        {
            embeddings.Add(item.ToFloats());
        }

        // Calculate total tokens used
        int totalTokens = response.Value.Usage.TotalTokenCount;

        return new EmbeddingResponse(embeddings, totalTokens);
    }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return _tokenizer.CountTokens(text);
    }

    /// <inheritdoc/>
    public int GetMaxStringLength(string text, int maxTokens)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (maxTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "Max tokens must be greater than zero.");
        }

        // If the text is already within the limit, return the full length
        var totalTokens = CountTokens(text);
        if (totalTokens <= maxTokens)
        {
            return text.Length;
        }

        // Binary search to find the maximum string length that fits within the token limit
        int left = 0;
        int right = text.Length;
        int result = 0;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            var substring = text.Substring(0, mid);
            var tokens = CountTokens(substring);

            if (tokens <= maxTokens)
            {
                result = mid;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return result;
    }
}
