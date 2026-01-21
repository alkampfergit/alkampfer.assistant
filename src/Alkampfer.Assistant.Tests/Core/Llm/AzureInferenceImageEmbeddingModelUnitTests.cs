using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Llm;
using Alkampfer.Assistant.Interfaces.Llm;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Unit tests for image embedding functionality in AzureInferenceEmbeddingModel.
/// </summary>
public class AzureInferenceImageEmbeddingModelUnitTests
{
    [Fact]
    public void ImplementsIImageEmbeddingModel()
    {
        // Verify that AzureInferenceEmbeddingModel implements both interfaces
        var model = new AzureInferenceEmbeddingModel(
            "https://test.azure.com",
            "test-key",
            "test-model"
        );

        Assert.IsAssignableFrom<IEmbeddingModel>(model);
        Assert.IsAssignableFrom<IImageEmbeddingModel>(model);
    }

    [Fact]
    public async Task GenerateImageEmbeddingAsync_WithNullPath_ThrowsArgumentNullException()
    {
        var model = new AzureInferenceEmbeddingModel(
            "https://test.azure.com",
            "test-key",
            "test-model"
        );

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await model.GenerateImageEmbeddingAsync(null!));
    }

    [Fact]
    public async Task GenerateImageEmbeddingAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var model = new AzureInferenceEmbeddingModel(
            "https://test.azure.com",
            "test-key",
            "test-model"
        );

        await Assert.ThrowsAsync<System.IO.FileNotFoundException>(
            async () => await model.GenerateImageEmbeddingAsync("/nonexistent/image.png"));
    }

    [Fact]
    public async Task GenerateImageEmbeddingAsync_WithUninitializedImageClient_ThrowsInvalidOperationException()
    {
        // Create model using test constructor without image client
        var mockClient = TestHelpers.CreateMockEmbeddingsClient(new[] { new[] { 1.0f, 2.0f, 3.0f } });
        var tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel("text-embedding-3-small");
        var model = new AzureInferenceEmbeddingModel(mockClient, "test-model", tokenizer, imageClient: null);

        // Create a temporary test file
        var tempFile = System.IO.Path.GetTempFileName();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await model.GenerateImageEmbeddingAsync(tempFile));
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }
}

/// <summary>
/// Helper class for creating mock clients used in tests.
/// </summary>
internal static class TestHelpers
{
    public static Azure.AI.Inference.EmbeddingsClient CreateMockEmbeddingsClient(float[][] embeddings)
    {
        // Create mock response data
        var embeddingItems = System.Linq.Enumerable.Range(0, embeddings.Length)
            .Select(idx => new
            {
                embedding = embeddings[idx],
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

        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(responseData);
        var binaryData = BinaryData.FromString(jsonResponse);

        // Create a mock HTTP transport using Azure.Core
        var mockResponse = new MockResponse(200, "OK");
        mockResponse.SetContent(binaryData.ToString());

        var mockTransport = new MockHttpClientTransport(mockResponse);
        var options = new Azure.AI.Inference.AzureAIInferenceClientOptions
        {
            Transport = mockTransport
        };

        var client = new Azure.AI.Inference.EmbeddingsClient(
            new Uri("https://test.inference.azure.com"),
            new Azure.AzureKeyCredential("test-key"),
            options);

        return client;
    }

    private class MockHttpClientTransport : HttpPipelineTransport
    {
        private readonly Response _response;

        public MockHttpClientTransport(Response response)
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
