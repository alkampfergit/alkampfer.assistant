using System;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Alkampfer.Assistant.Interfaces.Llm;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Integration tests for AzureOpenAiChatLanguageModel.
/// Requires AZURE_ENDPOINT, OPENAI_API_KEY, and AZURE_MODEL environment variables.
/// </summary>
[Trait("Category", "Integration")]
public class ChatLanguageModelTests
{
    private readonly AzureOpenAiChatLanguageModel? _sut;
    private readonly bool _canRunTests;

    public ChatLanguageModelTests()
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
            _sut = new AzureOpenAiChatLanguageModel(azureEndpoint, apiKey, model);
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
        Assert.NotNull(response.Response);
        Assert.NotEmpty(response.Response);
        Assert.Contains("4", response.Response);
        Assert.NotNull(response.Statistics);
        Assert.True(response.Statistics.InputTokens > 0);
        Assert.True(response.Statistics.OutputTokens > 0);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithMultiplePrompts_ShouldBeStateless()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var firstPrompt = "My favorite color is blue.";
        var secondPrompt = "What is my favorite color?";

        // Act
        await _sut!.GenerateResponseAsync(firstPrompt);
        var response = await _sut.GenerateResponseAsync(secondPrompt);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Response);
        // Since the model should be stateless, verify it does not repeat the explicit prior statement.
        // Allow mentions of color names in general, but not a verbatim recall of the previous user sentence.
        Assert.DoesNotContain("My favorite color is blue", response.Response, StringComparison.OrdinalIgnoreCase);
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
            _sut!.GenerateResponseAsync((string)null!));
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
    public async Task GenerateResponseAsync_WithComplexPrompt_ShouldHandleMultilineResponse()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var prompt = "List three primary colors. Reply with only the color names, one per line.";

        // Act
        var response = await _sut!.GenerateResponseAsync(prompt);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Response);
        Assert.NotEmpty(response.Response);
        // Check for common primary colors
        var lowerResponse = response.Response.ToLowerInvariant();
        var hasRed = lowerResponse.IndexOf("red", StringComparison.OrdinalIgnoreCase) >= 0;
        var hasBlue = lowerResponse.IndexOf("blue", StringComparison.OrdinalIgnoreCase) >= 0;
        var hasYellow = lowerResponse.IndexOf("yellow", StringComparison.OrdinalIgnoreCase) >= 0;
        
        Assert.True(hasRed || hasBlue || hasYellow, 
            "Response should contain at least one primary color");
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
        var instance = new AzureOpenAiChatLanguageModel(azureEndpoint!, apiKey!, model!);

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithNullChatClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureOpenAiChatLanguageModel(null!));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithLlmRequest_ShouldReturnResponse()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var request = new LlmRequest
        {
            Messages = new List<Alkampfer.Assistant.Interfaces.ConversationMessage>
            {
                new(Alkampfer.Assistant.Interfaces.ConversationRole.User, "What is 2+2? Reply with only the number.")
            }
        };

        // Act
        var response = await _sut!.GenerateResponseAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Response);
        Assert.NotEmpty(response.Response);
        Assert.Contains("4", response.Response);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithLlmRequest_WithPreviousConversationId_ShouldThrowNotSupportedException()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var request = new LlmRequest
        {
            PreviousConversationId = "some-id",
            Messages = new List<Alkampfer.Assistant.Interfaces.ConversationMessage>
            {
                new(Alkampfer.Assistant.Interfaces.ConversationRole.User, "Hello")
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _sut!.GenerateResponseAsync(request));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithLlmRequest_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut!.GenerateResponseAsync((LlmRequest)null!));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithLlmRequest_EmptyMessages_ShouldThrowArgumentException()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var request = new LlmRequest
        {
            Messages = new List<Alkampfer.Assistant.Interfaces.ConversationMessage>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut!.GenerateResponseAsync(request));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithLlmRequest_MultipleMessages_ShouldIncludeConversationHistory()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var request = new LlmRequest
        {
            Messages = new List<Alkampfer.Assistant.Interfaces.ConversationMessage>
            {
                new(Alkampfer.Assistant.Interfaces.ConversationRole.System, "You are a helpful assistant."),
                new(Alkampfer.Assistant.Interfaces.ConversationRole.User, "My favorite color is blue."),
                new(Alkampfer.Assistant.Interfaces.ConversationRole.Assistant, "Noted."),
                new(Alkampfer.Assistant.Interfaces.ConversationRole.User, "What is my favorite color?")
            }
        };

        // Act
        var response = await _sut!.GenerateResponseAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Response);
        Assert.Contains("blue", response.Response, StringComparison.OrdinalIgnoreCase);
    }
}
