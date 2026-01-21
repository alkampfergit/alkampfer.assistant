using System;
using System.IO;
using Alkampfer.Assistant.Interfaces.Llm;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.ML.Tokenizers;

namespace Alkampfer.Assistant.Core.Llm;

/// <summary>
/// Azure AI Inference embedding model implementation using Azure.AI.Inference SDK.
/// Uses Tiktoken tokenizer for token counting and text truncation.
/// Supports both text and image embeddings.
/// </summary>
public class AzureInferenceEmbeddingModel : IEmbeddingModel, IImageEmbeddingModel
{
    private readonly EmbeddingsClient _client;
    private readonly ImageEmbeddingsClient _imageClient;
    private readonly Tokenizer _tokenizer;
    private readonly string _modelName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureInferenceEmbeddingModel"/> class.
    /// </summary>
    /// <param name="azureInferenceEndpoint">The Azure AI Inference endpoint URL.</param>
    /// <param name="azureInferenceCredential">The API key for authentication.</param>
    /// <param name="modelName">The model name to use (e.g., "Cohere-embed-v3-multilingual").</param>
    public AzureInferenceEmbeddingModel(string azureInferenceEndpoint, string azureInferenceCredential, string modelName)
    {
        ArgumentNullException.ThrowIfNull(azureInferenceEndpoint);
        ArgumentNullException.ThrowIfNull(azureInferenceCredential);
        ArgumentNullException.ThrowIfNull(modelName);

        var endpoint = new Uri(azureInferenceEndpoint);
        var credential = new AzureKeyCredential(azureInferenceCredential);

        // Create client options with a policy to add the extra-parameters header
        // This allows the SDK to pass through extra parameters like 'stream' without errors
        var clientOptions = new AzureAIInferenceClientOptions();
        clientOptions.AddPolicy(new ExtraParametersPolicy(), HttpPipelinePosition.PerCall);

        _client = new EmbeddingsClient(endpoint, credential, clientOptions);
        _imageClient = new ImageEmbeddingsClient(endpoint, credential, clientOptions);
        _modelName = modelName;
        _tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureInferenceEmbeddingModel"/> class.
    /// This constructor is primarily used for testing with mock clients.
    /// </summary>
    /// <param name="client">The EmbeddingsClient instance.</param>
    /// <param name="modelName">The model name to use.</param>
    /// <param name="tokenizer">The tokenizer to use for token counting.</param>
    /// <param name="imageClient">The ImageEmbeddingsClient instance (optional for text-only testing).</param>
    public AzureInferenceEmbeddingModel(EmbeddingsClient client, string modelName, Tokenizer tokenizer, ImageEmbeddingsClient? imageClient = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modelName);
        ArgumentNullException.ThrowIfNull(tokenizer);

        _client = client;
        _imageClient = imageClient!; // Nullable for backward compatibility with existing tests
        _modelName = modelName;
        _tokenizer = tokenizer;
    }

    /// <inheritdoc/>
    public async Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        var textList = texts.ToList();
        if (textList.Count == 0)
        {
            throw new ArgumentException("At least one text must be provided.", nameof(texts));
        }

        options ??= EmbeddingOptions.Default;

        var requestOptions = new EmbeddingsOptions(textList)
        {
            Model = _modelName,
        };

        // Map TextType to Azure SDK's EmbeddingInputType
        // Azure AI Inference SDK supports Document and Query input types
        if (options.TextType != EmbeddingTextType.Neutral)
        {
            requestOptions.InputType = options.TextType switch
            {
                EmbeddingTextType.Document => EmbeddingInputType.Document,
                EmbeddingTextType.Query => EmbeddingInputType.Query,
                // Image type is not supported by Azure AI Inference SDK
                _ => null
            };
        }

        // Set dimensions if specified (supported by Cohere and other models)
        if (options.Dimensions.HasValue)
        {
            requestOptions.Dimensions = options.Dimensions.Value;
        }

        var response = await _client.EmbedAsync(requestOptions, cancellationToken);

        if (response.Value == null || response.Value.Data == null)
        {
            throw new InvalidOperationException($"Unexpected response: Value or Data is null. Raw response: {response.GetRawResponse()?.Content}");
        }

        var embeddings = new List<ReadOnlyMemory<float>>();
        foreach (var item in response.Value.Data)
        {
            var embeddingBinaryData = item.Embedding;
            var vector = embeddingBinaryData.ToObjectFromJson<float[]>();
            if (vector != null)
            {
                embeddings.Add(vector);
            }
        }

        // Calculate total tokens using tokenizer since Azure Inference may not return usage
        int totalTokens = textList.Sum(t => CountTokens(t));

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

    /// <inheritdoc/>
    public async Task<EmbeddingResponse> GenerateImageEmbeddingAsync(
        string imagePath,
        string? imageFormat = null,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imagePath);

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}", imagePath);
        }

        if (_imageClient == null)
        {
            throw new InvalidOperationException("ImageEmbeddingsClient is not initialized. This instance was created for text-only testing.");
        }

        options ??= EmbeddingOptions.Default;

        // Infer format from extension if not provided
        var format = imageFormat ?? options.ImageFormat ?? Path.GetExtension(imagePath).TrimStart('.');

        // Load image and create input
        var imageInput = ImageEmbeddingInput.Load(
            imageFilePath: imagePath,
            imageFormat: format
        );

        var input = new List<ImageEmbeddingInput> { imageInput };
        var requestOptions = new ImageEmbeddingsOptions(input)
        {
            Model = _modelName
        };

        // Set dimensions if specified
        if (options.Dimensions.HasValue)
        {
            requestOptions.Dimensions = options.Dimensions.Value;
        }

        var response = await _imageClient.EmbedAsync(requestOptions, cancellationToken);

        // Extract embedding
        var embeddings = new List<ReadOnlyMemory<float>>();
        var vectorEmbedding = response.Value.Data[0].Embedding;
        var vector = vectorEmbedding.ToObjectFromJson<float[]>();

        if (vector != null)
        {
            embeddings.Add(vector);
        }

        // For images, we don't have a meaningful token count
        // Return 0 to indicate this metric is not applicable for images
        int totalTokens = 0;

        return new EmbeddingResponse(embeddings, totalTokens);
    }
}

/// <summary>
/// HTTP pipeline policy that adds the 'extra-parameters: pass-through' header to requests.
/// This is required for Azure AI Inference endpoints to accept requests from the SDK,
/// which may include extra parameters like 'stream' that the embedding endpoint doesn't recognize.
/// </summary>
internal class ExtraParametersPolicy : HttpPipelinePolicy
{
    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        message.Request.Headers.SetValue("extra-parameters", "pass-through");
        ProcessNext(message, pipeline);
    }

    public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        message.Request.Headers.SetValue("extra-parameters", "pass-through");
        return ProcessNextAsync(message, pipeline);
    }
}
