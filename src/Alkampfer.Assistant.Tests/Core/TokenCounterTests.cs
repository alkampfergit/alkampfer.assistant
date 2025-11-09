using Alkampfer.Assistant.Core;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

public class TokenCounterTests
{
    private readonly TokenCounter _tokenCounter;

    public TokenCounterTests()
    {
        _tokenCounter = new TokenCounter();
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Return_Positive_Count_For_Simple_Text(string model)
    {
        // Arrange
        var text = "Hello, world!";

        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.True(count > 0, $"Token count should be positive for model {model}");
    }

    [Theory]
    [InlineData("gpt-4o", "Hello, world!", 4)]
    [InlineData("gpt-4o-mini", "Hello, world!", 4)]
    [InlineData("gpt-5-mini", "Hello, world!", 4)]
    [InlineData("gpt-4o", "The quick brown fox jumps over the lazy dog", 9)]
    [InlineData("gpt-4o-mini", "The quick brown fox jumps over the lazy dog", 9)]
    [InlineData("gpt-5-mini", "The quick brown fox jumps over the lazy dog", 9)]
    public void CountTokens_Should_Return_Expected_Count_For_Known_Texts(string model, string text, int expectedCount)
    {
        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.Equal(expectedCount, count);
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Return_Zero_For_Empty_String(string model)
    {
        // Arrange
        var text = "";

        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.Equal(0, count);
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Handle_Long_Text(string model)
    {
        // Arrange
        var text = string.Concat(Enumerable.Repeat("This is a test sentence. ", 100));

        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.True(count > 400, $"Long text should have more than 400 tokens for model {model}");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Handle_Special_Characters(string model)
    {
        // Arrange
        var text = "Hello! @#$%^&*() {}[] <> ~`";

        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.True(count > 0, $"Text with special characters should have positive token count for model {model}");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Handle_Unicode_Characters(string model)
    {
        // Arrange
        var text = "Hello 世界! 🌍🚀";

        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.True(count > 0, $"Text with Unicode characters should have positive token count for model {model}");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Handle_Multiline_Text(string model)
    {
        // Arrange
        var text = @"Line 1
Line 2
Line 3";

        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.True(count > 4, $"Multiline text should have more than 4 tokens for model {model}");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    [InlineData("gpt-4")]
    [InlineData("gpt-3.5-turbo")]
    public void CountTokens_Should_Support_Common_GPT_Models(string model)
    {
        // Arrange
        var text = "Test message";

        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.True(count > 0, $"Model {model} should be supported");
    }

    [Fact]
    public void CountTokens_Should_Throw_For_Unsupported_Model()
    {
        // Arrange
        var text = "Test message";
        var unsupportedModel = "claude-3";

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _tokenCounter.CountTokens(unsupportedModel, text));
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Throw_When_ModelIdentifier_Is_Null(string _)
    {
        // Arrange
        var text = "Test message";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _tokenCounter.CountTokens(null!, text));
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Throw_When_Text_Is_Null(string model)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _tokenCounter.CountTokens(model, null!));
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-5-mini")]
    public void CountTokens_Should_Be_Case_Insensitive_For_Model_Names(string model)
    {
        // Arrange
        var text = "Test message";
        var upperCaseModel = model.ToUpper();

        // Act
        var count1 = _tokenCounter.CountTokens(model, text);
        var count2 = _tokenCounter.CountTokens(upperCaseModel, text);

        // Assert
        Assert.Equal(count1, count2);
    }

    [Fact]
    public void CountTokens_Should_Return_Same_Count_For_Same_Text_Across_Multiple_Calls()
    {
        // Arrange
        var text = "Consistent token counting test";
        var model = "gpt-4o";

        // Act
        var count1 = _tokenCounter.CountTokens(model, text);
        var count2 = _tokenCounter.CountTokens(model, text);
        var count3 = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.Equal(count1, count2);
        Assert.Equal(count2, count3);
    }

    [Theory]
    [InlineData("gpt-4o", "artificial intelligence", 3)]
    [InlineData("gpt-4o-mini", "artificial intelligence", 3)]
    [InlineData("gpt-5-mini", "artificial intelligence", 3)]
    public void CountTokens_Should_Tokenize_Compound_Words_Correctly(string model, string text, int expectedCount)
    {
        // Act
        var count = _tokenCounter.CountTokens(model, text);

        // Assert
        Assert.Equal(expectedCount, count);
    }
}
