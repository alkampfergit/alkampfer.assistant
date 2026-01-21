using System;
using System.ClientModel;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Llm;
using Microsoft.ML.Tokenizers;
using Moq;
using OpenAI;
using OpenAI.Embeddings;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Unit tests for OpenAiEmbeddingModel using mocks.
/// Tests implementation-specific behavior without requiring real API credentials.
/// Does not inherit from base class - these are focused unit tests for the class itself.
/// </summary>
public class OpenAIEmbeddingModelUnitTests
{
    [Fact]
    public void Constructor_WithValidAzureParameters_ShouldCreateInstance()
    {
        // Act
        var instance = new OpenAiEmbeddingModel(
            "https://test.openai.azure.com",
            "test-key",
            "text-embedding-3-small"
        );

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithValidOpenAIParameters_ShouldCreateInstance()
    {
        // Act
        var instance = new OpenAiEmbeddingModel(
            "test-key",
            "text-embedding-3-small"
        );

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithNullAzureEndpoint_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel(null!, "key", "model"));
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel("https://test.com", null!, "model"));
    }

    [Fact]
    public void Constructor_WithNullModelName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel("https://test.com", "key", null!));
    }

    [Fact]
    public void Constructor_WithNullEmbeddingClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel((EmbeddingClient)null!, "model"));
    }

    [Fact]
    public void Constructor_WithNullModelNameInClientConstructor_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<EmbeddingClient>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel(mockClient.Object, null!));
    }
}
