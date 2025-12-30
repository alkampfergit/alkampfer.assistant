using System;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Interfaces.Memories;

public class MetadataValueTests
{
    [Fact]
    public void ImplicitConversion_FromString_ShouldCreateStringMetadataValue()
    {
        // Arrange & Act
        MetadataValue value = "test string";

        // Assert
        Assert.NotNull(value);
        Assert.Equal("test string", value.AsString());
        Assert.Null(value.AsInt());
        Assert.Null(value.AsDouble());
        Assert.Null(value.AsDateTime());
    }

    [Fact]
    public void ImplicitConversion_FromInt_ShouldCreateIntMetadataValue()
    {
        // Arrange & Act
        MetadataValue value = 42;

        // Assert
        Assert.NotNull(value);
        Assert.Equal(42, value.AsInt());
        Assert.Null(value.AsString());
        Assert.Null(value.AsDouble());
        Assert.Null(value.AsDateTime());
    }

    [Fact]
    public void ImplicitConversion_FromDouble_ShouldCreateDoubleMetadataValue()
    {
        // Arrange & Act
        MetadataValue value = 3.14;

        // Assert
        Assert.NotNull(value);
        Assert.Equal(3.14, value.AsDouble());
        Assert.Null(value.AsString());
        Assert.Null(value.AsInt());
        Assert.Null(value.AsDateTime());
    }

    [Fact]
    public void ImplicitConversion_FromDateTime_ShouldCreateDateTimeMetadataValue()
    {
        // Arrange
        var dateTime = new DateTime(2025, 12, 30, 10, 30, 0, DateTimeKind.Utc);

        // Act
        MetadataValue value = dateTime;

        // Assert
        Assert.NotNull(value);
        Assert.Equal(dateTime, value.AsDateTime());
        Assert.Null(value.AsString());
        Assert.Null(value.AsInt());
        Assert.Null(value.AsDouble());
    }

    [Fact]
    public void AsString_WhenNotStringType_ShouldReturnNull()
    {
        // Arrange
        MetadataValue intValue = 42;
        MetadataValue doubleValue = 3.14;
        MetadataValue dateTimeValue = DateTime.UtcNow;

        // Act & Assert
        Assert.Null(intValue.AsString());
        Assert.Null(doubleValue.AsString());
        Assert.Null(dateTimeValue.AsString());
    }

    [Fact]
    public void AsInt_WhenNotIntType_ShouldReturnNull()
    {
        // Arrange
        MetadataValue stringValue = "test";
        MetadataValue doubleValue = 3.14;
        MetadataValue dateTimeValue = DateTime.UtcNow;

        // Act & Assert
        Assert.Null(stringValue.AsInt());
        Assert.Null(doubleValue.AsInt());
        Assert.Null(dateTimeValue.AsInt());
    }

    [Fact]
    public void AsDouble_WhenNotDoubleType_ShouldReturnNull()
    {
        // Arrange
        MetadataValue stringValue = "test";
        MetadataValue intValue = 42;
        MetadataValue dateTimeValue = DateTime.UtcNow;

        // Act & Assert
        Assert.Null(stringValue.AsDouble());
        Assert.Null(intValue.AsDouble());
        Assert.Null(dateTimeValue.AsDouble());
    }

    [Fact]
    public void AsDateTime_WhenNotDateTimeType_ShouldReturnNull()
    {
        // Arrange
        MetadataValue stringValue = "test";
        MetadataValue intValue = 42;
        MetadataValue doubleValue = 3.14;

        // Act & Assert
        Assert.Null(stringValue.AsDateTime());
        Assert.Null(intValue.AsDateTime());
        Assert.Null(doubleValue.AsDateTime());
    }

    [Fact]
    public void Value_ShouldReturnUnderlyingValue()
    {
        // Arrange & Act
        MetadataValue stringValue = "test";
        MetadataValue intValue = 42;
        MetadataValue doubleValue = 3.14;
        var dateTime = DateTime.UtcNow;
        MetadataValue dateTimeValue = dateTime;

        // Assert
        Assert.Equal("test", stringValue.Value);
        Assert.Equal(42, intValue.Value);
        Assert.Equal(3.14, doubleValue.Value);
        Assert.Equal(dateTime, dateTimeValue.Value);
    }
}
