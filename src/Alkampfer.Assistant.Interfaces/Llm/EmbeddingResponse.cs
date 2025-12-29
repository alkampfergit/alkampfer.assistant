namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Represents the response from an embedding generation request,
/// containing embedding vectors and token usage statistics.
/// </summary>
public class EmbeddingResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingResponse"/> class.
    /// </summary>
    /// <param name="embeddings">The list of embedding vectors, in the same order as the input texts.</param>
    /// <param name="totalTokens">The total number of tokens consumed across all input texts.</param>
    public EmbeddingResponse(
        IReadOnlyList<ReadOnlyMemory<float>> embeddings,
        int totalTokens)
    {
        if (totalTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalTokens), "Total tokens cannot be negative.");
        }

        Embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
        TotalTokens = totalTokens;
    }

    /// <summary>
    /// Gets the list of embedding vectors, one for each input text, in the same order as the input.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<float>> Embeddings { get; }

    /// <summary>
    /// Gets the total number of tokens consumed across all input texts.
    /// </summary>
    public int TotalTokens { get; }

    /// <summary>
    /// Gets the first embedding vector, or an empty memory if there are no results.
    /// Convenience property for single-input requests.
    /// </summary>
    public ReadOnlyMemory<float> FirstEmbedding => Embeddings.Count > 0 ? Embeddings[0] : ReadOnlyMemory<float>.Empty;
}
