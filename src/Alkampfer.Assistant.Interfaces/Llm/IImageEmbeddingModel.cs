namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Represents an embedding model that can generate vector embeddings from image inputs.
/// </summary>
public interface IImageEmbeddingModel
{
    /// <summary>
    /// Generates an embedding for a single image file.
    /// </summary>
    /// <param name="imagePath">The file path to the image.</param>
    /// <param name="imageFormat">The image format (e.g., "png", "jpg"). If null, format is inferred from file extension.</param>
    /// <param name="options">Options for configuring the embedding generation. If null, default options are used.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An <see cref="EmbeddingResponse"/> containing a single embedding vector.</returns>
    /// <remarks>
    /// This method processes a single image at a time. The returned embedding response will contain exactly one embedding.
    /// Not all embedding models support all options; unsupported options may be ignored by some implementations.
    /// </remarks>
    Task<EmbeddingResponse> GenerateImageEmbeddingAsync(
        string imagePath,
        string? imageFormat = null,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default);
}
