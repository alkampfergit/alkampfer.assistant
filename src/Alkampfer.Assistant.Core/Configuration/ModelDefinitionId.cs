using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Core.Configuration;

/// <summary>
/// Strongly-typed identity for ModelDefinition entities.
/// </summary>
public sealed class ModelDefinitionId : Identity
{
    /// <summary>
    /// Initializes a new instance from a string value (e.g., "ModelDefinition/123").
    /// Used for deserialization.
    /// </summary>
    public ModelDefinitionId(string value) : base(value)
    {
    }

    /// <summary>
    /// Initializes a new instance from a numeric ID.
    /// </summary>
    public ModelDefinitionId(long numericId) : base(numericId)
    {
    }
}
