using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

/// <summary>
/// Strongly-typed identity for TestEntity entities used in repository tests.
/// </summary>
public sealed class TestEntityId : Identity
{
    /// <summary>
    /// Initializes a new instance from a string value (e.g., "TestEntity/123").
    /// Used for deserialization.
    /// </summary>
    public TestEntityId(string value) : base(value)
    {
    }

    /// <summary>
    /// Initializes a new instance from a numeric ID.
    /// </summary>
    public TestEntityId(long numericId) : base(numericId)
    {
    }
}
