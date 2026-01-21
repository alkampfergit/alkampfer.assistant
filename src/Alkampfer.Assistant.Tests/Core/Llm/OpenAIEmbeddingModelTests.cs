using System;
using System.Collections.Generic;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Alkampfer.Assistant.Interfaces.Llm;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Contract tests for OpenAiEmbeddingModel.
/// Inherits from EmbeddingModelTestsBase to ensure the implementation adheres to IEmbeddingModel contract.
/// Provides 10 contract tests for token counting, string truncation, and input validation.
/// For implementation-specific unit tests with mocks, see OpenAIEmbeddingModelUnitTests.
/// </summary>
public class OpenAIEmbeddingModelTests : EmbeddingModelTestsBase
{
    protected override IEmbeddingModel CreateEmbeddingModel()
    {
        // Create a minimal instance for contract testing
        // This doesn't need to make real API calls - contract tests only verify interface behavior
        return new OpenAiEmbeddingModel(
            "https://test.openai.azure.com",
            "test-key",
            "text-embedding-3-small"
        );
    }
}

/// <summary>
/// Integration tests for OpenAiEmbeddingModel.
/// Requires AZURE_ENDPOINT, OPENAI_API_KEY, and AZURE_EMBEDDING_MODEL environment variables.
/// </summary>
[Trait("Category", "Integration")]
public class OpenAIEmbeddingModelIntegrationTests : IntegrationEmbeddingModelTestsBase
{
    public OpenAIEmbeddingModelIntegrationTests()
        : base(new Dictionary<string, string>
        {
            { "AZURE_ENDPOINT", "Azure OpenAI endpoint URL" },
            { "OPENAI_API_KEY", "Azure OpenAI API key" },
            { "AZURE_EMBEDDING_MODEL", "Azure embedding model name" }
        })
    {
        // Load environment variables from .env file if available
        DotEnv.Load();
    }

    protected override IEmbeddingModel CreateEmbeddingModelFromCredentials(Dictionary<string, string> credentials)
    {
        return new OpenAiEmbeddingModel(
            credentials["AZURE_ENDPOINT"],
            credentials["OPENAI_API_KEY"],
            credentials["AZURE_EMBEDDING_MODEL"]
        );
    }
}
