namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Represents an embedding model that can generate vector embeddings from text inputs.
/// </summary>
public interface IEmbeddingModel
{
    /// <summary>
    /// Generates embeddings for multiple text inputs.
    /// </summary>
    /// <param name="texts">The collection of texts to embed.</param>
    /// <param name="textType">The type of text being embedded (e.g., document, query). Some models may use this to optimize embeddings.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An <see cref="EmbeddingResponse"/> containing the embedding vectors in the same order as the input texts.</returns>
    /// <remarks>
    /// The returned embeddings are in the same order as the input texts.
    /// Not all embedding models support the <paramref name="textType"/> parameter; it may be ignored by some implementations.
    /// </remarks>
    Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        EmbeddingTextType textType = EmbeddingTextType.Neutral,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of tokens in the provided text.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>The number of tokens in the text.</returns>
    int CountTokens(string text);

    /// <summary>
    /// Calculates the maximum string length that fits within the specified token limit.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <param name="maxTokens">The maximum number of tokens allowed.</param>
    /// <returns>The length of the string (in characters) that fits within the token limit, or the full text length if it's already within the limit.</returns>
    /// <remarks>
    /// This method is useful for truncating text to fit within model token limits.
    /// It returns the character position where truncation should occur.
    /// </remarks>
    int GetMaxStringLength(string text, int maxTokens);
}
