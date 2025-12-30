using Alkampfer.Assistant.Interfaces.Llm;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.Core.Memories;

/// <summary>
/// A simple text chunker that splits text by phrase boundaries (sentence terminators)
/// while respecting a maximum token limit per chunk.
/// </summary>
public class SimpleTextChunker : ITextChunker
{
    private readonly IEmbeddingModel _embeddingModel;
    private readonly int _maxTokensPerChunk;
    private readonly char[] _phraseTerminators;

    /// <summary>
    /// Default phrase terminators for Western languages (period, question mark, exclamation mark, newline).
    /// </summary>
    public static readonly char[] DefaultPhraseTerminators = { '.', '?', '!', '\n' };

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTextChunker"/> class.
    /// </summary>
    /// <param name="embeddingModel">The embedding model used to count tokens.</param>
    /// <param name="maxTokensPerChunk">The maximum number of tokens allowed per chunk.</param>
    /// <param name="phraseTerminators">Optional array of characters that terminate phrases. If null, uses default Western language terminators.</param>
    public SimpleTextChunker(
        IEmbeddingModel embeddingModel,
        int maxTokensPerChunk,
        char[]? phraseTerminators = null)
    {
        _embeddingModel = embeddingModel ?? throw new ArgumentNullException(nameof(embeddingModel));

        if (maxTokensPerChunk <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTokensPerChunk), "Max tokens per chunk must be greater than zero.");
        }

        _maxTokensPerChunk = maxTokensPerChunk;
        _phraseTerminators = phraseTerminators ?? DefaultPhraseTerminators;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TextChunk>> ChunkAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<IReadOnlyList<TextChunk>>(Array.Empty<TextChunk>());
        }

        var chunks = new List<TextChunk>();
        var currentChunkText = string.Empty;
        var currentChunkTokens = 0;
        var position = 0;

        while (position < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Find the next phrase boundary
            var nextTerminator = FindNextPhraseTerminator(text, position);
            var phraseEnd = nextTerminator >= 0 ? nextTerminator + 1 : text.Length;

            // Extract the phrase (including the terminator if present)
            var phrase = text.Substring(position, phraseEnd - position);
            var phraseTokens = _embeddingModel.CountTokens(phrase);

            // Check if adding this phrase would exceed the limit
            var combinedText = currentChunkText + phrase;
            var combinedTokens = string.IsNullOrEmpty(currentChunkText)
                ? phraseTokens
                : _embeddingModel.CountTokens(combinedText);

            if (combinedTokens <= _maxTokensPerChunk)
            {
                // Phrase fits in current chunk
                currentChunkText = combinedText;
                currentChunkTokens = combinedTokens;
                position = phraseEnd;
            }
            else
            {
                // Phrase doesn't fit
                if (string.IsNullOrEmpty(currentChunkText))
                {
                    // Current chunk is empty, but phrase is too long
                    // We must break the phrase even without a terminator
                    var truncatedLength = _embeddingModel.GetMaxStringLength(phrase, _maxTokensPerChunk);

                    if (truncatedLength == 0)
                    {
                        // Even a single character exceeds the limit - this shouldn't happen in practice
                        // but we need to handle it to avoid infinite loop
                        truncatedLength = 1;
                    }

                    var truncatedPhrase = phrase.Substring(0, truncatedLength);
                    var truncatedTokens = _embeddingModel.CountTokens(truncatedPhrase);

                    chunks.Add(new TextChunk(truncatedPhrase, truncatedTokens));
                    position += truncatedLength;
                }
                else
                {
                    // Save current chunk and start a new one with this phrase
                    chunks.Add(new TextChunk(currentChunkText, currentChunkTokens));
                    currentChunkText = string.Empty;
                    currentChunkTokens = 0;
                    // Don't advance position - we'll process this phrase in the next iteration
                }
            }
        }

        // Add the last chunk if it has content
        if (!string.IsNullOrEmpty(currentChunkText))
        {
            chunks.Add(new TextChunk(currentChunkText, currentChunkTokens));
        }

        return Task.FromResult<IReadOnlyList<TextChunk>>(chunks);
    }

    /// <summary>
    /// Finds the next phrase terminator in the text starting from the given position.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="startPosition">The position to start searching from.</param>
    /// <returns>The index of the next phrase terminator, or -1 if none is found.</returns>
    private int FindNextPhraseTerminator(string text, int startPosition)
    {
        for (int i = startPosition; i < text.Length; i++)
        {
            if (Array.IndexOf(_phraseTerminators, text[i]) >= 0)
            {
                return i;
            }
        }
        return -1;
    }
}
