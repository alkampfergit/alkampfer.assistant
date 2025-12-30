using System;
using System.Collections.Generic;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Interfaces.Memories;

public class VectorSearchResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateResult()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" }
        };

        // Act
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("id-123", result.Id);
        Assert.Equal("doc-456", result.DocumentId);
        Assert.Equal(0.95, result.Score);
        Assert.Single(result.Metadata);
    }

    [Fact]
    public void Constructor_WithNullId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VectorSearchResult(null!, "doc-456", 0.95, metadata));
    }

    [Fact]
    public void Constructor_WithNullDocumentId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VectorSearchResult("id-123", null!, 0.95, metadata));
    }

    [Fact]
    public void Constructor_WithNullMetadata_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VectorSearchResult("id-123", "doc-456", 0.95, null!));
    }

    [Fact]
    public void GetMetadata_WithExistingKey_ShouldReturnMetadataValue()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadata("title");

        // Assert
        Assert.NotNull(value);
        Assert.Equal("Document Title", value.AsString());
    }

    [Fact]
    public void GetMetadata_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadata("nonexistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsString_WithExistingStringKey_ShouldReturnString()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsString("title");

        // Assert
        Assert.Equal("Document Title", value);
    }

    [Fact]
    public void GetMetadataAsString_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "count", 42 }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsString("count");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsString_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsString("nonexistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsInt_WithExistingIntKey_ShouldReturnInt()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "count", 42 }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsInt("count");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetMetadataAsInt_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsInt("title");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsInt_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsInt("nonexistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsDouble_WithExistingDoubleKey_ShouldReturnDouble()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "relevance", 0.95 }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsDouble("relevance");

        // Assert
        Assert.Equal(0.95, value);
    }

    [Fact]
    public void GetMetadataAsDouble_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "count", 42 }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsDouble("count");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsDouble_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsDouble("nonexistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsDateTime_WithExistingDateTimeKey_ShouldReturnDateTime()
    {
        // Arrange
        var dateTime = new DateTime(2025, 12, 30, 10, 30, 0, DateTimeKind.Utc);
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "createdAt", dateTime }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsDateTime("createdAt");

        // Assert
        Assert.Equal(dateTime, value);
    }

    [Fact]
    public void GetMetadataAsDateTime_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsDateTime("title");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsDateTime_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsDateTime("nonexistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsBool_WithExistingBoolKey_ShouldReturnBool()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "isActive", true }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsBool("isActive");

        // Assert
        Assert.Equal(true, value);
    }

    [Fact]
    public void GetMetadataAsBool_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsBool("title");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetMetadataAsBool_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act
        var value = result.GetMetadataAsBool("nonexistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void CompleteExample_WithAllMetadataTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var dateTime = new DateTime(2025, 12, 30, 10, 30, 0, DateTimeKind.Utc);
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" },
            { "chunkIndex", 0 },
            { "relevance", 0.95 },
            { "isActive", true },
            { "createdAt", dateTime }
        };

        // Act
        var result = new VectorSearchResult("chunk-123", "doc-456", 0.87, metadata);

        // Assert
        Assert.Equal("chunk-123", result.Id);
        Assert.Equal("doc-456", result.DocumentId);
        Assert.Equal(0.87, result.Score);
        Assert.Equal(5, result.Metadata.Count);

        Assert.Equal("Document Title", result.GetMetadataAsString("title"));
        Assert.Equal(0, result.GetMetadataAsInt("chunkIndex"));
        Assert.Equal(0.95, result.GetMetadataAsDouble("relevance"));
        Assert.Equal(true, result.GetMetadataAsBool("isActive"));
        Assert.Equal(dateTime, result.GetMetadataAsDateTime("createdAt"));
    }

    [Fact]
    public void Score_ShouldAcceptVariousValues()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>();

        // Act
        var result1 = new VectorSearchResult("id-1", "doc-1", 0.0, metadata);
        var result2 = new VectorSearchResult("id-2", "doc-2", 0.5, metadata);
        var result3 = new VectorSearchResult("id-3", "doc-3", 1.0, metadata);
        var result4 = new VectorSearchResult("id-4", "doc-4", -1.0, metadata);

        // Assert
        Assert.Equal(0.0, result1.Score);
        Assert.Equal(0.5, result2.Score);
        Assert.Equal(1.0, result3.Score);
        Assert.Equal(-1.0, result4.Score);
    }

    [Fact]
    public void Metadata_ShouldBeReadOnly()
    {
        // Arrange
        var metadata = new Dictionary<string, MetadataValue>
        {
            { "title", "Document Title" }
        };
        var result = new VectorSearchResult("id-123", "doc-456", 0.95, metadata);

        // Act & Assert
        // The Metadata property should return IReadOnlyDictionary
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, MetadataValue>>(result.Metadata);
    }
}
