using Alkampfer.Assistant.Interfaces.Llm;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Llm;

/// <summary>
/// Abstract base class for embedding model contract tests.
/// Provides a suite of tests that verify core IEmbeddingModel interface behavior.
/// 
/// This class uses the Abstract Factory pattern to allow concrete test classes
/// to specify which embedding model implementation to test, while ensuring
/// all implementations adhere to the same behavioral contracts.
/// 
/// Test Coverage:
/// - Token counting (CountTokens): validation, empty strings, null handling
/// - String truncation (GetMaxStringLength): within limit, exceeding limit, edge cases
/// - Input validation: null collections, empty collections
/// 
/// Usage:
/// 1. Inherit from this class in your concrete test class
/// 2. Implement CreateEmbeddingModel() to return your specific implementation
/// 3. The base class will automatically run all contract tests
/// 4. Add implementation-specific tests in your derived class
/// 
/// Note: These are contract/unit tests. For integration tests that require
/// real API credentials, see IntegrationEmbeddingModelTestsBase.
/// </summary>
public abstract class EmbeddingModelTestsBase
{
    /// <summary>
    /// Factory method to create the embedding model instance for testing.
    /// Concrete test classes must implement this to provide their specific implementation.
    /// 
    /// The returned instance should be suitable for testing (may use mocks or test data).
    /// For integration tests with real APIs, use IntegrationEmbeddingModelTestsBase instead.
    /// </summary>
    /// <returns>An IEmbeddingModel instance to test.</returns>
    protected abstract IEmbeddingModel CreateEmbeddingModel();

    [Fact]
    public void CountTokens_WithValidText_ShouldReturnPositiveCount()
    {
        // Arrange
        var model = CreateEmbeddingModel();
        var text = "Hello, world!";

        // Act
        var count = model.CountTokens(text);

        // Assert
        Assert.True(count > 0, "Token count should be positive for non-empty text");
    }

    [Fact]
    public void CountTokens_WithEmptyString_ShouldReturnZero()
    {
        // Arrange
        var model = CreateEmbeddingModel();
        var text = string.Empty;

        // Act
        var count = model.CountTokens(text);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void CountTokens_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var model = CreateEmbeddingModel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => model.CountTokens(null!));
    }

    [Fact]
    public void GetMaxStringLength_WithTextWithinLimit_ShouldReturnFullLength()
    {
        // Arrange
        var model = CreateEmbeddingModel();
        var text = "Hello";
        var tokenCount = model.CountTokens(text);
        var maxTokens = tokenCount + 10; // More tokens than needed

        // Act
        var maxLength = model.GetMaxStringLength(text, maxTokens);

        // Assert
        Assert.Equal(text.Length, maxLength);
    }

    [Fact]
    public void GetMaxStringLength_WithTextExceedingLimit_ShouldReturnTruncatedLength()
    {
        // Arrange
        var model = CreateEmbeddingModel();
        var text = "This is a longer text that will be truncated to fit within the token limit.";
        var maxTokens = 5; // Very low limit

        // Act
        var maxLength = model.GetMaxStringLength(text, maxTokens);

        // Assert
        Assert.True(maxLength < text.Length, "Truncated length should be less than full text length");
        Assert.True(maxLength > 0, "Truncated length should be greater than zero");
        
        // Verify that the truncated text is within token limit
        var truncatedText = text.Substring(0, maxLength);
        var truncatedTokens = model.CountTokens(truncatedText);
        Assert.True(truncatedTokens <= maxTokens, $"Truncated text has {truncatedTokens} tokens, expected <= {maxTokens}");
    }

    [Fact]
    public void GetMaxStringLength_WithZeroMaxTokens_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var model = CreateEmbeddingModel();
        var text = "Hello";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetMaxStringLength(text, 0));
    }

    [Fact]
    public void GetMaxStringLength_WithNegativeMaxTokens_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var model = CreateEmbeddingModel();
        var text = "Hello";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetMaxStringLength(text, -1));
    }

    [Fact]
    public void GetMaxStringLength_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var model = CreateEmbeddingModel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => model.GetMaxStringLength(null!, 10));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var model = CreateEmbeddingModel();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await model.GenerateEmbeddingsAsync(null!, options: null));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyCollection_ShouldThrowArgumentException()
    {
        // Arrange
        var model = CreateEmbeddingModel();
        var emptyTexts = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await model.GenerateEmbeddingsAsync(emptyTexts));
    }
}
