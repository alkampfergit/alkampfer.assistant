namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Represents parameters for a K-Nearest Neighbors (KNN) vector search.
/// </summary>
/// <param name="VectorKey">The name/key of the vector field to search (e.g., "embedding", "title_embedding").</param>
/// <param name="QueryVector">The query vector to find similar vectors for.</param>
/// <param name="TopK">The maximum number of results to return.</param>
/// <param name="NumCandidates">
/// Optional number of candidates to consider during the KNN search.
/// If null, a sensible default will be calculated (typically max(100, TopK * 10)).
/// Higher values improve recall but increase query time.
/// </param>
public sealed record VectorSearchParams(
    string VectorKey,
    float[] QueryVector,
    int TopK,
    int? NumCandidates = null);
