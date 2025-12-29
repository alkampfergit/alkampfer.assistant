namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Specifies the type of text being embedded, which may influence
/// how the embedding model processes the input.
/// </summary>
/// <remarks>
/// Some embedding models (like text-embedding-3 from OpenAI) don't distinguish
/// between these types. Others may use this information to optimize embeddings
/// for specific use cases like retrieval or classification.
/// </remarks>
public enum EmbeddingTextType
{
    /// <summary>
    /// Neutral text with no specific purpose indicated.
    /// Use this when the embedding will be used for general purposes.
    /// </summary>
    Neutral,

    /// <summary>
    /// Document text that will be stored and searched.
    /// Some models optimize embeddings for document storage and retrieval.
    /// </summary>
    Document,

    /// <summary>
    /// Query text used to search for similar documents.
    /// Some models optimize embeddings for query matching.
    /// </summary>
    Query,

    /// <summary>
    /// Image content or image description text.
    /// For models that support multimodal embeddings.
    /// </summary>
    Image
}
