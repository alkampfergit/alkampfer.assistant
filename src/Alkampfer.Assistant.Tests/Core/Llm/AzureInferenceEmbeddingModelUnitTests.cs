using System;
using System.ClientModel.Primitives;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Llm;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Unit tests for AzureInferenceEmbeddingModel using mocks.
/// Tests implementation-specific behavior without requiring real API credentials.
/// Does not inherit from base class - these are focused unit tests for the class itself.
/// </summary>
public class AzureInferenceEmbeddingModelUnitTests
{
    [Fact]
    public async Task GenerateEmbeddingsAsync_WithSingleText_ReturnsSingleEmbedding()
    {
        // Arrange
        var expectedEmbedding = new[] { 1.0f, 2.0f, 3.0f };
        var mockClient = CreateMockEmbeddingsClient(new[] { expectedEmbedding });
        var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
        var model = new AzureInferenceEmbeddingModel(mockClient, "test-model", tokenizer);
        var texts = new[] { "hello" };

        // Act
        var result = await model.GenerateEmbeddingsAsync(texts, options: null);

        // Assert
        Assert.Single(result.Embeddings);
        Assert.Equal(3, result.Embeddings[0].Length);
        Assert.Equal(expectedEmbedding, result.Embeddings[0].ToArray());
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMultipleTexts_ReturnsCorrectCountAndSameDimension()
    {
        // Arrange
        var embeddings = new[]
        {
            new[] { 1.0f, 2.0f, 3.0f },
            new[] { 4.0f, 5.0f, 6.0f },
            new[] { 7.0f, 8.0f, 9.0f }
        };
        var mockClient = CreateMockEmbeddingsClient(embeddings);
        var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
        var model = new AzureInferenceEmbeddingModel(mockClient, "test-model", tokenizer);
        var texts = new[] { "text one", "text two", "text three" };

        // Act
        var result = await model.GenerateEmbeddingsAsync(texts, options: null);

        // Assert
        Assert.Equal(3, result.Embeddings.Count);
        Assert.All(result.Embeddings, e => Assert.Equal(3, e.Length));
        
        // Verify order is preserved
        Assert.Equal(embeddings[0], result.Embeddings[0].ToArray());
        Assert.Equal(embeddings[1], result.Embeddings[1].ToArray());
        Assert.Equal(embeddings[2], result.Embeddings[2].ToArray());
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_CalculatesTokenCountUsingTokenizer()
    {
        // Arrange
        var mockClient = CreateMockEmbeddingsClient(new[] { new[] { 1.0f, 2.0f } });
        var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
        var model = new AzureInferenceEmbeddingModel(mockClient, "test-model", tokenizer);
        var text = "Hello, world!";
        var expectedTokens = tokenizer.CountTokens(text);

        // Act
        var result = await model.GenerateEmbeddingsAsync(new[] { text });

        // Assert
        Assert.Equal(expectedTokens, result.TotalTokens);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var instance = new AzureInferenceEmbeddingModel(
            "https://test.azure.com",
            "test-key",
            "test-model"
        );

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithNullEndpoint_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureInferenceEmbeddingModel(null!, "key", "model"));
    }

    [Fact]
    public void Constructor_WithNullCredential_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureInferenceEmbeddingModel("https://test.com", null!, "model"));
    }

    [Fact]
    public void Constructor_WithNullModelName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureInferenceEmbeddingModel("https://test.com", "key", null!));
    }

    [Fact]
    public void Constructor_WithNullClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
        Assert.Throws<ArgumentNullException>(() => 
            new AzureInferenceEmbeddingModel(null!, "model", tokenizer));
    }

    [Fact]
    public void Constructor_WithNullTokenizer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var mockClient = CreateMockEmbeddingsClient(new[] { new[] { 1.0f } });
        Assert.Throws<ArgumentNullException>(() => 
            new AzureInferenceEmbeddingModel(mockClient, "model", null!));
    }

    [Fact]
    public void Constructor_WithNullModelNameInClientConstructor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var mockClient = CreateMockEmbeddingsClient(new[] { new[] { 1.0f } });
        var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
        Assert.Throws<ArgumentNullException>(() => 
            new AzureInferenceEmbeddingModel(mockClient, null!, tokenizer));
    }

    /// <summary>
    /// Creates a mock EmbeddingsClient that returns deterministic embeddings.
    /// </summary>
    private static EmbeddingsClient CreateMockEmbeddingsClient(float[][] embeddings)
    {
        // Create mock response data
        var embeddingItems = embeddings.Select((e, idx) => 
            new
            {
                embedding = e,
                index = idx
            }).ToArray();

        var responseData = new
        {
            data = embeddingItems,
            model = "test-model",
            usage = new
            {
                prompt_tokens = 10,
                total_tokens = 10
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);
        var binaryData = BinaryData.FromString(jsonResponse);

        // Create a mock HTTP transport using Azure.Core
        var mockResponse = new MockResponse(200, "OK");
        mockResponse.SetContent(binaryData.ToString());

        var mockTransport = new MockHttpClientTransport(mockResponse);
        var options = new AzureAIInferenceClientOptions
        {
            Transport = mockTransport
        };

        var client = new EmbeddingsClient(
            new Uri("https://test.inference.azure.com"),
            new AzureKeyCredential("test-key"),
            options);

        return client;
    }

    /// <summary>
    /// Mock HTTP transport for testing.
    /// </summary>
    private class MockHttpClientTransport : HttpPipelineTransport
    {
        private readonly MockResponse _response;

        public MockHttpClientTransport(MockResponse response)
        {
            _response = response;
        }

        public override Request CreateRequest()
        {
            return new MockRequest();
        }

        public override void Process(HttpMessage message)
        {
            message.Response = _response;
        }

        public override ValueTask ProcessAsync(HttpMessage message)
        {
            message.Response = _response;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Mock HTTP request for testing.
    /// </summary>
    private class MockRequest : Request
    {
        private RequestContent? _content;
        private readonly RequestUriBuilder _uri = new RequestUriBuilder { Scheme = "https", Host = "test.com" };

        public override void Dispose() { }

        public override RequestMethod Method { get; set; } = RequestMethod.Post;
        public override RequestUriBuilder Uri => _uri;
        public override RequestContent? Content { get => _content; set => _content = value; }
        public override string ClientRequestId { get; set; } = Guid.NewGuid().ToString();

        protected override void AddHeader(string name, string value) { }
        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;
            return false;
        }
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = Enumerable.Empty<string>();
            return false;
        }
        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => Enumerable.Empty<HttpHeader>();
        protected override bool RemoveHeader(string name) => false;
    }

    /// <summary>
    /// Mock HTTP response for testing.
    /// </summary>
    private class MockResponse : Response
    {
        private readonly int _status;
        private readonly string _reasonPhrase;
        private string _content = string.Empty;

        public MockResponse(int status, string reasonPhrase)
        {
            _status = status;
            _reasonPhrase = reasonPhrase;
        }

        public void SetContent(string content)
        {
            _content = content;
        }

        public override int Status => _status;
        public override string ReasonPhrase => _reasonPhrase;
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = string.Empty;

        public override BinaryData Content => BinaryData.FromString(_content);

        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = Enumerable.Empty<string>();
            return false;
        }

        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => Enumerable.Empty<HttpHeader>();

        public override void Dispose() { }
    }
}
