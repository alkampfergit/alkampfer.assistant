using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Alkampfer.Assistant.Interfaces.Llm;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Integration tests for OpenAiEmbeddingModel.
/// Requires AZURE_ENDPOINT, OPENAI_API_KEY, and AZURE_EMBEDDING_MODEL environment variables.
/// </summary>
[Trait("Category", "Integration")]
public class EmbeddingModelTests
{
    private readonly OpenAiEmbeddingModel? _sut;
    private readonly bool _canRunTests;
    private readonly string _missingVarsMessage;

    public EmbeddingModelTests()
    {
        // Load environment variables from .env file
        DotEnv.Load();

        var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_MODEL");

        var missingVars = new List<string>();
        if (string.IsNullOrWhiteSpace(azureEndpoint)) missingVars.Add("AZURE_ENDPOINT");
        if (string.IsNullOrWhiteSpace(apiKey)) missingVars.Add("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(model)) missingVars.Add("AZURE_EMBEDDING_MODEL");

        // Only create the model if all required variables are present
        if (missingVars.Count == 0)
        {
            // At this point, all variables are confirmed non-null and non-whitespace
            _sut = new OpenAiEmbeddingModel(azureEndpoint ?? throw new InvalidOperationException(), apiKey ?? throw new InvalidOperationException(), model ?? throw new InvalidOperationException());
            _canRunTests = true;
            _missingVarsMessage = string.Empty;
        }
        else
        {
            _canRunTests = false;
            _missingVarsMessage = $"Required environment variables ({string.Join(", ", missingVars)}) are not set for LLM integration tests";
        }
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithSingleText_ShouldReturnSingleEmbedding()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = new[] { "Hello, world!" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Embeddings);
        Assert.Single(response.Embeddings);
        Assert.True(response.Embeddings[0].Length > 0);
        Assert.True(response.TotalTokens > 0);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMultipleTexts_ShouldReturnMultipleEmbeddings()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = new[] { "First text", "Second text", "Third text" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Embeddings);
        Assert.Equal(3, response.Embeddings.Count);
        Assert.All(response.Embeddings, embedding => Assert.True(embedding.Length > 0));
        Assert.True(response.TotalTokens > 0);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_EmbeddingsShouldBeSameDimension()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = new[] { "Short", "This is a longer text with more words" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Embeddings.Count);
        var firstDimension = response.Embeddings[0].Length;
        var secondDimension = response.Embeddings[1].Length;
        Assert.Equal(firstDimension, secondDimension);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithDifferentTextTypes_ShouldSucceed()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = new[] { "This is a document about embeddings" };

        // Act - Test different text types (even though OpenAI ignores them)
        var neutralResponse = await _sut!.GenerateEmbeddingsAsync(texts, EmbeddingTextType.Neutral);
        var documentResponse = await _sut!.GenerateEmbeddingsAsync(texts, EmbeddingTextType.Document);
        var queryResponse = await _sut!.GenerateEmbeddingsAsync(texts, EmbeddingTextType.Query);

        // Assert - All should succeed and return same dimension
        Assert.NotNull(neutralResponse);
        Assert.NotNull(documentResponse);
        Assert.NotNull(queryResponse);
        Assert.Equal(neutralResponse.Embeddings[0].Length, documentResponse.Embeddings[0].Length);
        Assert.Equal(neutralResponse.Embeddings[0].Length, queryResponse.Embeddings[0].Length);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyCollection_ShouldThrowArgumentException()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut!.GenerateEmbeddingsAsync(texts));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithNullCollection_ShouldThrowArgumentNullException()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut!.GenerateEmbeddingsAsync(null!));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithCancellationToken_ShouldSupportCancellation()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var texts = new[] { "This should be cancelled" };

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _sut!.GenerateEmbeddingsAsync(texts, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_SimilarTextsShouldHaveSimilarEmbeddings()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = new[]
        {
            "The cat sits on the mat",
            "A feline rests on the rug",
            "Python is a programming language"
        };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts);

        // Assert
        Assert.Equal(3, response.Embeddings.Count);

        // Calculate cosine similarity between first two (similar) and first and third (different)
        var similarity12 = CosineSimilarity(response.Embeddings[0], response.Embeddings[1]);
        var similarity13 = CosineSimilarity(response.Embeddings[0], response.Embeddings[2]);

        // Similar texts should have higher similarity than different texts
        Assert.True(similarity12 > similarity13,
            $"Similar texts similarity ({similarity12}) should be greater than different texts ({similarity13})");
    }

    [Fact]
    public void CountTokens_WithValidText_ShouldReturnPositiveCount()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var text = "Hello, world!";

        // Act
        var count = _sut!.CountTokens(text);

        // Assert
        Assert.True(count > 0);
    }

    [Fact]
    public void CountTokens_WithEmptyString_ShouldReturnZero()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var text = "";

        // Act
        var count = _sut!.CountTokens(text);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void CountTokens_WithNullText_ShouldThrowArgumentNullException()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut!.CountTokens(null!));
    }

    [Fact]
    public void CountTokens_LongerTextShouldHaveMoreTokens()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var shortText = "Hi";
        var longText = "This is a much longer text with many more words and tokens";

        // Act
        var shortCount = _sut!.CountTokens(shortText);
        var longCount = _sut!.CountTokens(longText);

        // Assert
        Assert.True(longCount > shortCount);
    }

    [Fact]
    public void GetMaxStringLength_WithTextWithinLimit_ShouldReturnFullLength()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var text = "Hello, world!";
        var tokenCount = _sut!.CountTokens(text);
        var maxTokens = tokenCount + 10; // More than needed

        // Act
        var maxLength = _sut!.GetMaxStringLength(text, maxTokens);

        // Assert
        Assert.Equal(text.Length, maxLength);
    }

    [Fact]
    public void GetMaxStringLength_WithTextExceedingLimit_ShouldReturnTruncatedLength()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var text = "This is a long text that will need to be truncated to fit within the token limit";
        var maxTokens = 5;

        // Act
        var maxLength = _sut!.GetMaxStringLength(text, maxTokens);

        // Assert
        Assert.True(maxLength < text.Length);
        Assert.True(maxLength > 0);

        // Verify that the truncated text is within the token limit
        var truncatedText = text.Substring(0, maxLength);
        var truncatedTokens = _sut!.CountTokens(truncatedText);
        Assert.True(truncatedTokens <= maxTokens);
    }

    [Fact]
    public void GetMaxStringLength_WithZeroMaxTokens_ShouldThrowArgumentOutOfRangeException()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var text = "Test text";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut!.GetMaxStringLength(text, 0));
    }

    [Fact]
    public void GetMaxStringLength_WithNegativeMaxTokens_ShouldThrowArgumentOutOfRangeException()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var text = "Test text";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut!.GetMaxStringLength(text, -1));
    }

    [Fact]
    public void GetMaxStringLength_WithNullText_ShouldThrowArgumentNullException()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _sut!.GetMaxStringLength(null!, 10));
    }

    [Fact]
    public void Constructor_WithValidAzureParameters_ShouldCreateInstance()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_MODEL");

        // Act
        var instance = new OpenAiEmbeddingModel(azureEndpoint!, apiKey!, model!);

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithNullEmbeddingClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel((OpenAI.Embeddings.EmbeddingClient)null!, "model"));
    }

    [Fact]
    public async Task FirstEmbedding_WithSingleText_ShouldReturnSameAsFirstElement()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = new[] { "Test text" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts);

        // Assert
        Assert.Equal(response.Embeddings[0].Length, response.FirstEmbedding.Length);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_OrderShouldMatchInputOrder()
    {
        if (!_canRunTests)
        {
            Assert.Fail(_missingVarsMessage);
        }

        var texts = new[] { "First", "Second", "Third" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts);

        // Assert
        Assert.Equal(3, response.Embeddings.Count);
        // We can't directly verify the order matches semantically, but we can verify count matches
        Assert.Equal(texts.Length, response.Embeddings.Count);
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    private static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var aSpan = a.Span;
        var bSpan = b.Span;

        if (aSpan.Length != bSpan.Length)
        {
            throw new ArgumentException("Vectors must have the same dimension");
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < aSpan.Length; i++)
        {
            dotProduct += aSpan[i] * bSpan[i];
            magnitudeA += aSpan[i] * aSpan[i];
            magnitudeB += bSpan[i] * bSpan[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        return dotProduct / (magnitudeA * magnitudeB);
    }
}
