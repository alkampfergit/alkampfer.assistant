using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.VectorStore;

/// <summary>
/// Represents a single result from a vector store query.
/// </summary>
public class VectorSearchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorSearchResult"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the document/record.</param>
    /// <param name="score">The similarity score (0.0 to 1.0, where 1.0 is most similar).</param>
    /// <param name="content">The content or text of the matched record.</param>
    public VectorSearchResult(string id, float score, string content)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID cannot be null or empty.", nameof(id));
        }

        if (score < 0.0f || score > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0.0 and 1.0.");
        }

        Id = id;
        Score = score;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// Gets the unique identifier of the document or record in the vector store.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the similarity score between the query and this result.
    /// </summary>
    /// <remarks>
    /// The score is normalized to a value between 0.0 and 1.0, where 1.0 represents the highest similarity.
    /// </remarks>
    public float Score { get; }

    /// <summary>
    /// Gets the content or text of the matched record.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets or sets optional metadata associated with this result.
    /// </summary>
    /// <remarks>
    /// This can include additional information such as document type, source, timestamps, tags, etc.
    /// The specific metadata fields depend on what was stored in the vector store.
    /// </remarks>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the embedding vector for this result.
    /// </summary>
    /// <remarks>
    /// This is optional and may not be populated by all implementations.
    /// Including the embedding can be useful for re-ranking or additional similarity calculations.
    /// The default value is <see cref="ReadOnlyMemory{T}.Empty"/> when not set.
    /// </remarks>
    public ReadOnlyMemory<float> Embedding { get; set; }
}
