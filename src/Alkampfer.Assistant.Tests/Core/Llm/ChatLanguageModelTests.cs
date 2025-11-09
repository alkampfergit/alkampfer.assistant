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
public class ChatLanguageModelTests : IDisposable
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
        Assert.NotEmpty(response);
        Assert.Contains("4", response);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithMultiplePrompts_ShouldMaintainConversationHistory()
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
        Assert.Contains("blue", response, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, _sut.History.Count); // 2 user messages + 2 assistant messages
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
    public async Task ClearHistory_ShouldResetConversationContext()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        var firstPrompt = "Remember that my name is Alice.";
        var secondPrompt = "What is my name?";

        // Act
        await _sut!.GenerateResponseAsync(firstPrompt);
        _sut.ClearHistory();
        var response = await _sut.GenerateResponseAsync(secondPrompt);

        // Assert
        Assert.NotNull(response);
        // The model should not know the name anymore
        Assert.DoesNotContain("Alice", response, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, _sut.History.Count); // Only the last interaction
    }

    [Fact]
    public void History_ShouldBeReadOnly()
    {
        // Arrange
        if (!_canRunTests)
        {
            return;
        }

        // Act
        var history = _sut!.History;

        // Assert
        Assert.NotNull(history);
        Assert.IsAssignableFrom<IReadOnlyList<OpenAI.Chat.ChatMessage>>(history);
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
        Assert.NotEmpty(response);
        // Check for common primary colors
        var lowerResponse = response.ToLowerInvariant();
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
        Assert.NotNull(instance.History);
        Assert.Empty(instance.History);
    }

    [Fact]
    public void Constructor_WithNullChatClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureOpenAiChatLanguageModel(null!));
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

    public void Dispose()
    {
        _sut?.ClearHistory();
    }
}
