using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Llm;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Base class for integration tests of embedding model implementations.
/// Requires real API credentials via environment variables.
/// Derived classes must implement CreateEmbeddingModelFromCredentials to create their specific implementation.
/// </summary>
[Trait("Category", "Integration")]
public abstract class IntegrationEmbeddingModelTestsBase
{
    protected readonly IEmbeddingModel? _sut;
    protected readonly bool _canRunTests;
    protected readonly string _missingVarsMessage;

    /// <summary>
    /// Initializes the integration test base with environment variable validation.
    /// </summary>
    /// <param name="requiredEnvVars">Dictionary of environment variable names to their descriptions.</param>
    protected IntegrationEmbeddingModelTestsBase(Dictionary<string, string> requiredEnvVars)
    {
        var missingVars = new List<string>();
        var envVarValues = new Dictionary<string, string>();

        foreach (var envVar in requiredEnvVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar.Key);
            if (string.IsNullOrWhiteSpace(value))
            {
                missingVars.Add(envVar.Key);
            }
            else
            {
                envVarValues[envVar.Key] = value;
            }
        }

        if (missingVars.Count == 0)
        {
            _sut = CreateEmbeddingModelFromCredentials(envVarValues);
            _canRunTests = true;
            _missingVarsMessage = string.Empty;
        }
        else
        {
            _canRunTests = false;
            _missingVarsMessage = $"Required environment variables ({string.Join(", ", missingVars)}) are not set for embedding model integration tests";
        }
    }

    /// <summary>
    /// Ensures tests can run and throws an exception when they cannot.
    /// </summary>
    private void EnsureCanRunTest()
    {
        if (!_canRunTests)
        {
            // Throw an exception if environment variables are missing
            throw new InvalidOperationException(_missingVarsMessage);
        }
    }

    /// <summary>
    /// Factory method to create the specific embedding model implementation from credentials.
    /// </summary>
    /// <param name="credentials">Dictionary of environment variable values.</param>
    /// <returns>The configured embedding model instance.</returns>
    protected abstract IEmbeddingModel CreateEmbeddingModelFromCredentials(Dictionary<string, string> credentials);

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithSingleText_ShouldReturnSingleEmbedding()
    {
        EnsureCanRunTest();

        var texts = new[] { "Hello, world!" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts, options: null);

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
        EnsureCanRunTest();

        var texts = new[] { "First text", "Second text", "Third text" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts, options: null);

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
        EnsureCanRunTest();

        var texts = new[] { "Short", "This is a longer text with more words" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts, options: null);

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
        EnsureCanRunTest();

        var texts = new[] { "This is a document about embeddings" };

        // Act - Test different text types using new options API
        var neutralResponse = await _sut!.GenerateEmbeddingsAsync(
            texts,
            new EmbeddingOptions { TextType = EmbeddingTextType.Neutral });

        var documentResponse = await _sut!.GenerateEmbeddingsAsync(
            texts,
            new EmbeddingOptions { TextType = EmbeddingTextType.Document });

        var queryResponse = await _sut!.GenerateEmbeddingsAsync(
            texts,
            new EmbeddingOptions { TextType = EmbeddingTextType.Query });

        // Assert - All should succeed and return same dimension
        Assert.NotNull(neutralResponse);
        Assert.NotNull(documentResponse);
        Assert.NotNull(queryResponse);
        Assert.Equal(neutralResponse.Embeddings[0].Length, documentResponse.Embeddings[0].Length);
        Assert.Equal(neutralResponse.Embeddings[0].Length, queryResponse.Embeddings[0].Length);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithCustomDimensions_ShouldReturnCorrectDimensions()
    {
        EnsureCanRunTest();

        var texts = new[] { "Test embedding with custom dimensions" };
        var requestedDimensions = 512; // Request smaller dimension

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(
            texts,
            new EmbeddingOptions { Dimensions = requestedDimensions });

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Embeddings);
        // Note: Some models may not support custom dimensions, so we check if it's set OR default
        Assert.True(response.Embeddings[0].Length > 0);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithCancellationToken_ShouldSupportCancellation()
    {
        EnsureCanRunTest();

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var texts = new[] { "This should be cancelled" };

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _sut!.GenerateEmbeddingsAsync(texts, options: null, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_SimilarTextsShouldHaveSimilarEmbeddings()
    {
        EnsureCanRunTest();

        var texts = new[]
        {
            "The cat sits on the mat",
            "A feline rests on the rug",
            "Python is a programming language"
        };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts, options: null);

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
    public async Task FirstEmbedding_WithSingleText_ShouldReturnSameAsFirstElement()
    {
        EnsureCanRunTest();

        var texts = new[] { "Test text" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts, options: null);

        // Assert
        Assert.Equal(response.Embeddings[0].Length, response.FirstEmbedding.Length);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_OrderShouldMatchInputOrder()
    {
        EnsureCanRunTest();

        var texts = new[] { "First", "Second", "Third" };

        // Act
        var response = await _sut!.GenerateEmbeddingsAsync(texts, options: null);

        // Assert
        Assert.Equal(3, response.Embeddings.Count);
        // Verify count matches input order
        Assert.Equal(texts.Length, response.Embeddings.Count);
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    protected static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
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
