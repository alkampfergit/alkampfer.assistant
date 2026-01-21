namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Options for configuring embedding generation behavior.
/// </summary>
/// <remarks>
/// This record provides configuration options for embedding generation,
/// including text type classification and output dimension control.
/// Not all embedding models support all options; unsupported options will be ignored.
/// </remarks>
public record EmbeddingOptions
{
    /// <summary>
    /// Gets the type of text being embedded (e.g., document, query).
    /// Some models may use this to optimize embeddings for specific use cases.
    /// </summary>
    /// <remarks>
    /// Default value is <see cref="EmbeddingTextType.Neutral"/>.
    /// Models like OpenAI's text-embedding-3 series may not use this parameter.
    /// </remarks>
    public EmbeddingTextType TextType { get; init; } = EmbeddingTextType.Neutral;

    /// <summary>
    /// Gets the desired number of dimensions for the output embeddings.
    /// </summary>
    /// <remarks>
    /// When specified, instructs the model to return embeddings with the specified dimensions.
    /// This is useful for reducing embedding size while maintaining most of the semantic information.
    /// Must be less than or equal to the model's maximum dimensions.
    /// When null, the model's default dimensions are used.
    /// Supported by OpenAI text-embedding-3 models and Azure AI Inference with compatible models.
    /// </remarks>
    public int? Dimensions { get; init; }

    /// <summary>
    /// Gets the image format for image embeddings (e.g., "png", "jpg").
    /// </summary>
    /// <remarks>
    /// Only used when generating image embeddings. Ignored for text embeddings.
    /// If null, the format is inferred from the file extension.
    /// Common formats: "png", "jpg", "jpeg", "gif", "bmp".
    /// </remarks>
    public string? ImageFormat { get; init; }

    /// <summary>
    /// Gets a default instance with standard settings.
    /// </summary>
    public static EmbeddingOptions Default { get; } = new();
}
