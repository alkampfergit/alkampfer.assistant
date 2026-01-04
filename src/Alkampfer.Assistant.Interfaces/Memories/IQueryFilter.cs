namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Represents a filter that can be applied to a vector query.
/// This is a marker interface for type safety - concrete implementations
/// are provided by specific vector store implementations.
/// </summary>
public interface IQueryFilter
{
    /// <summary>
    /// Gets the name of the field this filter applies to.
    /// </summary>
    string FieldName { get; }
}
