using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Memories;
using Alkampfer.Assistant.Interfaces.Llm;
using Alkampfer.Assistant.Interfaces.Memories;
using Moq;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Memories;

public class SimpleTextChunkerTests
{
    private readonly Mock<IEmbeddingModel> _mockEmbeddingModel;
    private readonly SimpleTextChunker _sut;

    public SimpleTextChunkerTests()
    {
        _mockEmbeddingModel = new Mock<IEmbeddingModel>();
        // Default: assume each character is roughly 1 token for simple tests
        _mockEmbeddingModel.Setup(m => m.CountTokens(It.IsAny<string>()))
            .Returns<string>(text => text.Length);
        _mockEmbeddingModel.Setup(m => m.GetMaxStringLength(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((text, maxTokens) => Math.Min(text.Length, maxTokens));

        _sut = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 50);
    }

    [Fact]
    public void Constructor_WithNullEmbeddingModel_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SimpleTextChunker(null!, 100));
    }

    [Fact]
    public void Constructor_WithZeroMaxTokens_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimpleTextChunker(_mockEmbeddingModel.Object, 0));
    }

    [Fact]
    public void Constructor_WithNegativeMaxTokens_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimpleTextChunker(_mockEmbeddingModel.Object, -10));
    }

    [Fact]
    public async Task ChunkAsync_WithNullText_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ChunkAsync(null!));
    }

    [Fact]
    public async Task ChunkAsync_WithEmptyText_ShouldReturnEmptyList()
    {
        // Arrange
        var text = "";

        // Act
        var result = await _sut.ChunkAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ChunkAsync_WithWhitespaceText_ShouldReturnEmptyList()
    {
        // Arrange
        var text = "   \t\n  ";

        // Act
        var result = await _sut.ChunkAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ChunkAsync_WithSingleShortSentence_ShouldReturnSingleChunk()
    {
        // Arrange
        var text = "Hello world.";

        // Act
        var result = await _sut.ChunkAsync(text);

        // Assert
        Assert.Single(result);
        Assert.Equal(text, result[0].Text);
        Assert.Equal(text.Length, result[0].TokenCount);
    }

    [Fact]
    public async Task ChunkAsync_WithMultipleSentencesFittingInOneChunk_ShouldReturnSingleChunk()
    {
        // Arrange
        var text = "First sentence. Second sentence."; // 33 chars, under 50 token limit

        // Act
        var result = await _sut.ChunkAsync(text);

        // Assert
        Assert.Single(result);
        Assert.Equal(text, result[0].Text);
    }

    [Fact]
    public async Task ChunkAsync_WithMultipleSentencesExceedingLimit_ShouldSplitIntoMultipleChunks()
    {
        // Arrange
        var text = "This is the first sentence that is quite long. This is the second sentence that is also quite long.";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 50);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.True(result.Count > 1, "Should create multiple chunks");
        Assert.All(result, chunk => Assert.True(chunk.TokenCount <= 50, "Each chunk should be within token limit"));
    }

    [Fact]
    public async Task ChunkAsync_ShouldRespectPhraseTerminators()
    {
        // Arrange
        var text = "Question one? Question two? Question three?";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 20);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.True(result.Count >= 2, "Should split at question marks");
        Assert.All(result, chunk => Assert.True(chunk.Text.EndsWith("?") || chunk == result.Last()));
    }

    [Fact]
    public async Task ChunkAsync_WithCustomTerminators_ShouldUseCustomTerminators()
    {
        // Arrange
        var text = "Part one; Part two; Part three";
        var customTerminators = new[] { ';' };
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 15, customTerminators);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.True(result.Count >= 2, "Should split at semicolons");
        foreach (var chunk in result.Take(result.Count - 1))
        {
            Assert.True(chunk.Text.Contains(';'), $"Chunk '{chunk.Text}' should contain semicolon");
        }
    }

    [Fact]
    public async Task ChunkAsync_WithNewlineTerminators_ShouldSplitOnNewlines()
    {
        // Arrange
        var text = "Line one\nLine two\nLine three";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 15);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.True(result.Count >= 2, "Should split at newlines");
    }

    [Fact]
    public async Task ChunkAsync_WithPhraseTooLong_ShouldBreakPhraseWithoutTerminator()
    {
        // Arrange
        var text = "ThisIsAVeryLongWordWithNoTerminatorsThatExceedsTheTokenLimit";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 20);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.True(result.Count > 1, "Should break long phrase into multiple chunks");
        Assert.All(result, chunk => Assert.True(chunk.TokenCount <= 20, "Each chunk should respect token limit"));
    }

    [Fact]
    public async Task ChunkAsync_ShouldPreserveChunkOrder()
    {
        // Arrange
        var text = "First. Second. Third. Fourth. Fifth.";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 15);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.Contains("First", result[0].Text);
        // Last chunk should contain "Fifth"
        Assert.Contains("Fifth", result[^1].Text);
    }

    [Fact]
    public async Task ChunkAsync_WithCancellationToken_ShouldSupportCancellation()
    {
        // Arrange
        var text = string.Concat(Enumerable.Repeat("Sentence. ", 1000));
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.ChunkAsync(text, cts.Token));
    }

    [Fact]
    public async Task ChunkAsync_WithRealisticTokenCounting_ShouldChunkCorrectly()
    {
        // Arrange - Simulate more realistic token counting
        var mockModel = new Mock<IEmbeddingModel>();
        mockModel.Setup(m => m.CountTokens(It.IsAny<string>()))
            .Returns<string>(text =>
            {
                // Rough approximation: ~4 chars per token
                return (int)Math.Ceiling(text.Length / 4.0);
            });
        mockModel.Setup(m => m.GetMaxStringLength(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((text, maxTokens) =>
            {
                var maxChars = maxTokens * 4;
                return Math.Min(text.Length, maxChars);
            });

        var text = "This is sentence one. This is sentence two. This is sentence three.";
        var chunker = new SimpleTextChunker(mockModel.Object, maxTokensPerChunk: 10);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, chunk =>
        {
            var tokens = mockModel.Object.CountTokens(chunk.Text);
            Assert.True(tokens <= 10, $"Chunk has {tokens} tokens, expected <= 10");
            Assert.Equal(tokens, chunk.TokenCount);
        });
    }

    [Fact]
    public async Task ChunkAsync_ShouldSetCorrectTokenCount()
    {
        // Arrange
        var text = "First. Second. Third.";

        // Act
        var result = await _sut.ChunkAsync(text);

        // Assert
        Assert.All(result, chunk =>
        {
            Assert.Equal(chunk.Text.Length, chunk.TokenCount);
        });
    }

    [Fact]
    public async Task ChunkAsync_WithMultipleTerminatorTypes_ShouldSplitAtAnyTerminator()
    {
        // Arrange
        var text = "Statement. Question? Exclamation! Another statement.";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 20);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.True(result.Count >= 2);
        // Each chunk (except possibly the last) should end with a terminator
        foreach (var chunk in result.Take(result.Count - 1))
        {
            var lastChar = chunk.Text[^1];
            Assert.True(lastChar == '.' || lastChar == '?' || lastChar == '!',
                $"Chunk should end with terminator, but ends with '{lastChar}'");
        }
    }

    [Fact]
    public async Task ChunkAsync_WithConsecutiveTerminators_ShouldHandleCorrectly()
    {
        // Arrange
        var text = "What?? Really!! Yes...";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 10);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.NotEmpty(result);
        // Should not create empty chunks
        Assert.All(result, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Text)));
    }

    [Fact]
    public async Task ChunkAsync_WithTextEndingWithoutTerminator_ShouldIncludeLastPhrase()
    {
        // Arrange
        var text = "First sentence. Second sentence without terminator";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 30);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.NotEmpty(result);
        var allText = string.Concat(result.Select(c => c.Text));
        Assert.Equal(text, allText);
    }

    [Fact]
    public void DefaultPhraseTerminators_ShouldContainStandardTerminators()
    {
        // Assert
        Assert.Contains('.', SimpleTextChunker.DefaultPhraseTerminators);
        Assert.Contains('?', SimpleTextChunker.DefaultPhraseTerminators);
        Assert.Contains('!', SimpleTextChunker.DefaultPhraseTerminators);
        Assert.Contains('\n', SimpleTextChunker.DefaultPhraseTerminators);
    }

    [Fact]
    public async Task ChunkAsync_WithVerySmallTokenLimit_ShouldStillCreateChunks()
    {
        // Arrange
        var text = "Hi.";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 1);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.NotEmpty(result);
        // Should handle gracefully even with very small limits
    }

    [Fact]
    public async Task ChunkAsync_AllChunksCombined_ShouldEqualOriginalText()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence. Fourth sentence.";

        // Act
        var result = await _sut.ChunkAsync(text);

        // Assert
        var reconstructed = string.Concat(result.Select(c => c.Text));
        Assert.Equal(text, reconstructed);
    }

    [Fact]
    public async Task ChunkAsync_WithComplexText_ShouldHandleAllTerminators()
    {
        // Arrange
        var text = @"What is this? This is a test!
This is a new line.
Another sentence. Final one?";
        var chunker = new SimpleTextChunker(_mockEmbeddingModel.Object, maxTokensPerChunk: 30);

        // Act
        var result = await chunker.ChunkAsync(text);

        // Assert
        Assert.NotEmpty(result);
        var reconstructed = string.Concat(result.Select(c => c.Text));
        Assert.Equal(text, reconstructed);
    }
}
