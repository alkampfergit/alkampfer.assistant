using System;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Alkampfer.Assistant.Interfaces.Llm;
using OpenAI.Responses;
using Xunit;

#pragma warning disable OPENAI001

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Integration tests for AzureOpenAiResponseLanguageModel.
/// Requires AZURE_ENDPOINT, OPENAI_API_KEY, and AZURE_MODEL environment variables.
/// </summary>
public class ResponseLanguageModelTests
{
    private readonly AzureOpenAiResponseLanguageModel? _sut;
    private readonly bool _canRunTests;

    public ResponseLanguageModelTests()
    {
        // Load environment variables from .env file
        DotEnv.Load();

        var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("AZURE_MODEL");

        // Only create the model if all required variables are present
        if (!string.IsNullOrWhiteSpace(azureEndpoint) &&
            !string.IsNullOrWhiteSpace(apiKey) &&
            !string.IsNullOrWhiteSpace(model))
        {
            _sut = new AzureOpenAiResponseLanguageModel(azureEndpoint, apiKey, model);
            _canRunTests = true;
        }
        else
        {
            _canRunTests = false;
        }
    }

    [Fact]
    public async Task GenerateResponseAsync_WithValidPrompt_ShouldReturnResponse()
    {
        // Arrange
        if (!_canRunTests)
        {
            // Skip test if credentials are not available
            return;
        }

        var prompt = "What is 2+2? Reply with only the number.";

        // Act
        var response = await _sut!.GenerateResponseAsync(prompt);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Contains("4", response);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithMultiplePrompts_ShouldBeStateless()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var firstPrompt = "My favorite color is green.";
        var secondPrompt = "What is my favorite color?";

        // Act
        await _sut!.GenerateResponseAsync(firstPrompt);
        var response = await _sut.GenerateResponseAsync(secondPrompt);

        // Assert
        Assert.NotNull(response);
        // Since it's stateless, the model should NOT remember the previous conversation
        Assert.DoesNotContain("green", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithNullPrompt_ShouldThrowArgumentException()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sut!.GenerateResponseAsync(null!));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithEmptyPrompt_ShouldThrowArgumentException()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sut!.GenerateResponseAsync(""));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithWhitespacePrompt_ShouldThrowArgumentException()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sut!.GenerateResponseAsync("   "));
    }


    [Fact]
    public async Task GenerateResponseAsync_WithCancellationToken_ShouldSupportCancellation()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _sut!.GenerateResponseAsync("This should be cancelled", cts.Token));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithReasoningTask_ShouldIncludeReasoningInResponse()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var prompt = "Solve this simple math problem: If I have 5 apples and buy 3 more, how many do I have?";

        // Act
        var response = await _sut!.GenerateResponseAsync(prompt);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Contains("8", response);
        // Response might include reasoning summary
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("AZURE_MODEL");

        // Act
        var instance = new AzureOpenAiResponseLanguageModel(azureEndpoint!, apiKey!, model!);

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithReasoningEffortLevel_ShouldCreateInstance()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("AZURE_MODEL");

        // Act
        var instanceLow = new AzureOpenAiResponseLanguageModel(
            azureEndpoint!, apiKey!, model!, ResponseReasoningEffortLevel.Low);
        var instanceMedium = new AzureOpenAiResponseLanguageModel(
            azureEndpoint!, apiKey!, model!, ResponseReasoningEffortLevel.Medium);
        var instanceHigh = new AzureOpenAiResponseLanguageModel(
            azureEndpoint!, apiKey!, model!, ResponseReasoningEffortLevel.High);

        // Assert
        Assert.NotNull(instanceLow);
        Assert.NotNull(instanceMedium);
        Assert.NotNull(instanceHigh);
    }

    [Fact]
    public void Constructor_WithNullResponseClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureOpenAiResponseLanguageModel(null!));
    }

    [Fact]
    public async Task ILanguageModel_Implementation_ShouldBeCompatible()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        ILanguageModel languageModel = _sut!;
        var prompt = "Say 'test' and nothing else.";

        // Act
        var response = await languageModel.GenerateResponseAsync(prompt);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithComplexPrompt_ShouldHandleMultilineResponse()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var prompt = "List three programming languages. Reply with only the language names, one per line.";

        // Act
        var response = await _sut!.GenerateResponseAsync(prompt);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        // Just verify we got a response with content
        Assert.True(response.Length > 5, "Response should contain meaningful content");
    }
}
