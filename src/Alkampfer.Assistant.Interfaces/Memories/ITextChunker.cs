namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Provides text chunking functionality to split text into smaller pieces.
/// </summary>
/// <remarks>
/// This is a generic interface that allows for different chunking strategies.
/// Implementations may use different approaches such as:
/// <list type="bullet">
/// <item><description>Token-based chunking: splitting by token count with configurable overlap</description></item>
/// <item><description>Sentence-based chunking: splitting by sentence boundaries</description></item>
/// <item><description>Paragraph-based chunking: splitting by paragraph boundaries</description></item>
/// <item><description>Semantic chunking: splitting by meaning or topic changes</description></item>
/// <item><description>Markdown-aware chunking: respecting markdown structure (headers, code blocks, etc.)</description></item>
/// </list>
/// The specific behavior and configuration options are determined by the concrete implementation.
/// </remarks>
public interface ITextChunker
{
    /// <summary>
    /// Splits the provided text into chunks.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="TextChunk"/> objects, in the order they appear in the original text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    /// <remarks>
    /// The returned chunks maintain the order of the original text.
    /// The chunking strategy (e.g., token limits, overlap, boundary detection) is determined by the implementation.
    /// </remarks>
    Task<IReadOnlyList<TextChunk>> ChunkAsync(
        string text,
        CancellationToken cancellationToken = default);
}
