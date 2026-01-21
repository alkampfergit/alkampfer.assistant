using System;
using System.Collections.Generic;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Alkampfer.Assistant.Interfaces.Llm;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Contract tests for AzureInferenceEmbeddingModel.
/// Inherits from EmbeddingModelTestsBase to ensure the implementation adheres to IEmbeddingModel contract.
/// Provides 10 contract tests for token counting, string truncation, and input validation.
/// For implementation-specific unit tests with mocks, see AzureInferenceEmbeddingModelUnitTests.
/// </summary>
public class AzureInferenceEmbeddingModelTests : EmbeddingModelTestsBase
{
    protected override IEmbeddingModel CreateEmbeddingModel()
    {
        DotEnv.Load();
        // Create a minimal instance for contract testing
        // This doesn't need to make real API calls - contract tests only verify interface behavior
        return new AzureInferenceEmbeddingModel(
            "https://test.inference.azure.com",
            "test-key",
            "test-model"
        );
    }
}

/// <summary>
/// Integration tests for AzureInferenceEmbeddingModel.
/// Uses the same environment variables as OpenAI tests (AZURE_ENDPOINT, OPENAI_API_KEY, AZURE_EMBEDDING_MODEL).
/// Azure AI Inference is compatible with Azure OpenAI endpoints, allowing both implementations to share configuration.
/// </summary>
[Trait("Category", "Integration")]
public class AzureInferenceEmbeddingModelIntegrationTests : IntegrationEmbeddingModelTestsBase
{
    public AzureInferenceEmbeddingModelIntegrationTests()
        : base(new Dictionary<string, string>
        {
            { "AZURE_INFERENCE_COHERE_EMBEDDING_URL", "Azure AI Inference endpoint URL" },
            { "AZURE_INFERENCE_COHERE_EMBEDDING_KEY", "Azure API key" },
            { "AZURE_INFERENCE_COHERE_EMBEDDING_MODEL", "Embedding model name" }
        })
    {
        DotEnv.Load();
    }

    protected override IEmbeddingModel CreateEmbeddingModelFromCredentials(Dictionary<string, string> credentials)
    {
        var embeddingModelVars = new AzureInferenceEmbeddingModel(
            credentials["AZURE_INFERENCE_COHERE_EMBEDDING_URL"],
            credentials["AZURE_INFERENCE_COHERE_EMBEDDING_KEY"],
            credentials["AZURE_INFERENCE_COHERE_EMBEDDING_MODEL"]
        );
        return embeddingModelVars;
    }
}
