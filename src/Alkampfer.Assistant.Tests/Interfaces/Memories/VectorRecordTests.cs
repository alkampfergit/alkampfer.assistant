using System;
using System.Linq;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Interfaces.Memories;

public class VectorRecordTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateRecord()
    {
        // Arrange & Act
        var record = VectorRecord.Create("id-123", "doc-456");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("id-123", record.Id);
        Assert.Equal("doc-456", record.DocumentId);
        Assert.Empty(record.Vectors);
        Assert.Empty(record.Metadata);
    }

    [Fact]
    public void Create_WithNullId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => VectorRecord.Create(null!, "doc-456"));
    }

    [Fact]
    public void Create_WithNullDocumentId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => VectorRecord.Create("id-123", null!));
    }

    [Fact]
    public void WithVector_ShouldAddVectorAndReturnSameInstance()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var result = record.WithVector("text", vector);

        // Assert
        Assert.Same(record, result); // Fluent interface returns same instance
        Assert.Single(record.Vectors);
        Assert.True(record.Vectors.ContainsKey("text"));
        Assert.Equal(vector, record.Vectors["text"]);
    }

    [Fact]
    public void WithVector_MultipleVectors_ShouldAddAllVectors()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var textVector = new float[] { 0.1f, 0.2f, 0.3f };
        var titleVector = new float[] { 0.4f, 0.5f, 0.6f };

        // Act
        record.WithVector("text", textVector)
              .WithVector("title", titleVector);

        // Assert
        Assert.Equal(2, record.Vectors.Count);
        Assert.Equal(textVector, record.Vectors["text"]);
        Assert.Equal(titleVector, record.Vectors["title"]);
    }

    [Fact]
    public void WithVector_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record.WithVector(null!, vector));
    }

    [Fact]
    public void WithVector_WithNullVector_ShouldThrowArgumentNullException()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record.WithVector("text", null!));
    }

    [Fact]
    public void WithVector_UpdateExistingKey_ShouldReplaceVector()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var firstVector = new float[] { 0.1f, 0.2f, 0.3f };
        var secondVector = new float[] { 0.4f, 0.5f, 0.6f };

        // Act
        record.WithVector("text", firstVector);
        record.WithVector("text", secondVector);

        // Assert
        Assert.Single(record.Vectors);
        Assert.Equal(secondVector, record.Vectors["text"]);
    }

    [Fact]
    public void WithMetadata_String_ShouldAddMetadataAndReturnSameInstance()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.WithMetadata("title", "Document Title");

        // Assert
        Assert.Same(record, result);
        Assert.Single(record.Metadata);
        Assert.Equal("Document Title", record.Metadata["title"].AsString());
    }

    [Fact]
    public void WithMetadata_Int_ShouldAddMetadataAndReturnSameInstance()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.WithMetadata("pageCount", 42);

        // Assert
        Assert.Same(record, result);
        Assert.Single(record.Metadata);
        Assert.Equal(42, record.Metadata["pageCount"].AsInt());
    }

    [Fact]
    public void WithMetadata_Double_ShouldAddMetadataAndReturnSameInstance()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.WithMetadata("relevance", 0.95);

        // Assert
        Assert.Same(record, result);
        Assert.Single(record.Metadata);
        Assert.Equal(0.95, record.Metadata["relevance"].AsDouble());
    }

    [Fact]
    public void WithMetadata_Bool_ShouldAddMetadataAndReturnSameInstance()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.WithMetadata("isActive", true);

        // Assert
        Assert.Same(record, result);
        Assert.Single(record.Metadata);
        Assert.Equal(true, record.Metadata["isActive"].AsBool());
    }

    [Fact]
    public void WithMetadata_DateTime_ShouldAddMetadataAndReturnSameInstance()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var dateTime = new DateTime(2025, 12, 30, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = record.WithMetadata("createdAt", dateTime);

        // Assert
        Assert.Same(record, result);
        Assert.Single(record.Metadata);
        Assert.Equal(dateTime, record.Metadata["createdAt"].AsDateTime());
    }

    [Fact]
    public void WithMetadata_MultipleMetadata_ShouldAddAllMetadata()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var dateTime = DateTime.UtcNow;

        // Act
        record.WithMetadata("title", "Document Title")
              .WithMetadata("pageCount", 42)
              .WithMetadata("relevance", 0.95)
              .WithMetadata("isActive", true)
              .WithMetadata("createdAt", dateTime);

        // Assert
        Assert.Equal(5, record.Metadata.Count);
        Assert.Equal("Document Title", record.Metadata["title"].AsString());
        Assert.Equal(42, record.Metadata["pageCount"].AsInt());
        Assert.Equal(0.95, record.Metadata["relevance"].AsDouble());
        Assert.Equal(true, record.Metadata["isActive"].AsBool());
        Assert.Equal(dateTime, record.Metadata["createdAt"].AsDateTime());
    }

    [Fact]
    public void WithMetadata_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata(null!, "value"));
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata(null!, 42));
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata(null!, 3.14));
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata(null!, true));
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata(null!, DateTime.UtcNow));
    }

    [Fact]
    public void WithMetadata_String_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata("key", (string)null!));
    }

    [Fact]
    public void WithMetadata_UpdateExistingKey_ShouldReplaceMetadata()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        record.WithMetadata("count", 10);
        record.WithMetadata("count", 20);

        // Assert
        Assert.Single(record.Metadata);
        Assert.Equal(20, record.Metadata["count"].AsInt());
    }

    [Fact]
    public void GetVector_WithExistingKey_ShouldReturnVector()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        record.WithVector("text", vector);

        // Act
        var result = record.GetVector("text");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vector, result);
    }

    [Fact]
    public void GetVector_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetVector("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadata_WithExistingKey_ShouldReturnMetadataValue()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("title", "Document Title");

        // Act
        var result = record.GetMetadata("title");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Document Title", result.AsString());
    }

    [Fact]
    public void GetMetadata_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetMetadata("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsString_WithExistingStringKey_ShouldReturnString()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("title", "Document Title");

        // Act
        var result = record.GetMetadataAsString("title");

        // Assert
        Assert.Equal("Document Title", result);
    }

    [Fact]
    public void GetMetadataAsString_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("count", 42);

        // Act
        var result = record.GetMetadataAsString("count");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsString_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetMetadataAsString("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsInt_WithExistingIntKey_ShouldReturnInt()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("count", 42);

        // Act
        var result = record.GetMetadataAsInt("count");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void GetMetadataAsInt_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("title", "Document Title");

        // Act
        var result = record.GetMetadataAsInt("title");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsInt_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetMetadataAsInt("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsDouble_WithExistingDoubleKey_ShouldReturnDouble()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("relevance", 0.95);

        // Act
        var result = record.GetMetadataAsDouble("relevance");

        // Assert
        Assert.Equal(0.95, result);
    }

    [Fact]
    public void GetMetadataAsDouble_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("count", 42);

        // Act
        var result = record.GetMetadataAsDouble("count");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsDouble_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetMetadataAsDouble("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsDateTime_WithExistingDateTimeKey_ShouldReturnDateTime()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var dateTime = new DateTime(2025, 12, 30, 10, 30, 0, DateTimeKind.Utc);
        record.WithMetadata("createdAt", dateTime);

        // Act
        var result = record.GetMetadataAsDateTime("createdAt");

        // Assert
        Assert.Equal(dateTime, result);
    }

    [Fact]
    public void GetMetadataAsDateTime_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("title", "Document Title");

        // Act
        var result = record.GetMetadataAsDateTime("title");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsDateTime_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetMetadataAsDateTime("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsBool_WithExistingBoolKey_ShouldReturnBool()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("isActive", true);

        // Act
        var result = record.GetMetadataAsBool("isActive");

        // Assert
        Assert.Equal(true, result);
    }

    [Fact]
    public void GetMetadataAsBool_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("title", "Document Title");

        // Act
        var result = record.GetMetadataAsBool("title");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsBool_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetMetadataAsBool("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void WithMetadata_Keywords_ShouldAddMetadataAndReturnSameInstance()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var keywords = new[] { "AI", "Machine Learning", "NLP" };

        // Act
        var result = record.WithMetadata("tags", keywords);

        // Assert
        Assert.Same(record, result);
        Assert.Single(record.Metadata);
        var retrievedKeywords = record.Metadata["tags"].AsKeywords();
        Assert.NotNull(retrievedKeywords);
        Assert.Equal(keywords, retrievedKeywords);
    }

    [Fact]
    public void WithMetadata_Keywords_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var keywords = new[] { "tag1", "tag2" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata(null!, keywords));
    }

    [Fact]
    public void WithMetadata_Keywords_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record.WithMetadata("tags", (string[])null!));
    }

    [Fact]
    public void GetMetadataAsKeywords_WithExistingKeywordsKey_ShouldReturnKeywords()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var keywords = new[] { "AI", "ML", "DL" };
        record.WithMetadata("tags", keywords);

        // Act
        var result = record.GetMetadataAsKeywords("tags");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(keywords, result);
    }

    [Fact]
    public void GetMetadataAsKeywords_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        record.WithMetadata("title", "Document Title");

        // Act
        var result = record.GetMetadataAsKeywords("title");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMetadataAsKeywords_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");

        // Act
        var result = record.GetMetadataAsKeywords("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void WithMetadata_EmptyKeywords_ShouldAddMetadata()
    {
        // Arrange
        var record = VectorRecord.Create("id-123", "doc-456");
        var keywords = Array.Empty<string>();

        // Act
        record.WithMetadata("tags", keywords);

        // Assert
        var result = record.GetMetadataAsKeywords("tags");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void FluentInterface_CompleteExample_ShouldWorkCorrectly()
    {
        // Arrange
        var dateTime = new DateTime(2025, 12, 30, 10, 30, 0, DateTimeKind.Utc);
        var textVector = new float[] { 0.1f, 0.2f, 0.3f };
        var titleVector = new float[] { 0.4f, 0.5f, 0.6f };

        // Act
        var record = VectorRecord.Create("chunk-123", "doc-456")
            .WithVector("text", textVector)
            .WithVector("title", titleVector)
            .WithMetadata("chunkIndex", 0)
            .WithMetadata("content", "This is the text content")
            .WithMetadata("relevance", 0.95)
            .WithMetadata("isActive", true)
            .WithMetadata("createdAt", dateTime);

        // Assert
        Assert.Equal("chunk-123", record.Id);
        Assert.Equal("doc-456", record.DocumentId);
        Assert.Equal(2, record.Vectors.Count);
        Assert.Equal(5, record.Metadata.Count);

        Assert.Equal(textVector, record.GetVector("text"));
        Assert.Equal(titleVector, record.GetVector("title"));

        Assert.Equal(0, record.GetMetadataAsInt("chunkIndex"));
        Assert.Equal("This is the text content", record.GetMetadataAsString("content"));
        Assert.Equal(0.95, record.GetMetadataAsDouble("relevance"));
        Assert.Equal(true, record.GetMetadataAsBool("isActive"));
        Assert.Equal(dateTime, record.GetMetadataAsDateTime("createdAt"));
    }

    [Fact]
    public void FluentInterface_WithKeywords_ShouldWorkCorrectly()
    {
        // Arrange
        var keywords = new[] { "AI", "Machine Learning", "Deep Learning" };
        var textVector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var record = VectorRecord.Create("chunk-456", "doc-789")
            .WithVector("text", textVector)
            .WithMetadata("tags", keywords)
            .WithMetadata("title", "AI Document")
            .WithMetadata("count", 5);

        // Assert
        Assert.Equal("chunk-456", record.Id);
        Assert.Equal("doc-789", record.DocumentId);
        Assert.Single(record.Vectors);
        Assert.Equal(3, record.Metadata.Count);

        Assert.Equal(textVector, record.GetVector("text"));
        var retrievedKeywords = record.GetMetadataAsKeywords("tags");
        Assert.NotNull(retrievedKeywords);
        Assert.Equal(keywords, retrievedKeywords);
        Assert.Equal("AI Document", record.GetMetadataAsString("title"));
        Assert.Equal(5, record.GetMetadataAsInt("count"));
    }
}
