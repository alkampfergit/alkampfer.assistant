namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Represents a single chunk of text with its token count.
/// </summary>
public class TextChunk
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextChunk"/> class.
    /// </summary>
    /// <param name="text">The chunk text.</param>
    /// <param name="tokenCount">The number of tokens in this chunk.</param>
    public TextChunk(string text, int tokenCount)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));

        if (tokenCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenCount), "Token count cannot be negative.");
        }

        TokenCount = tokenCount;
    }

    /// <summary>
    /// Gets the text content of this chunk.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the number of tokens in this chunk.
    /// </summary>
    public int TokenCount { get; }
}
