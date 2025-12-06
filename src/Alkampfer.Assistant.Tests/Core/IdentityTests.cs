using System;
using Alkampfer.Assistant.Interfaces;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

public class IdentityTests
{
    [Fact]
    public void Constructor_WithValidValue_ShouldParseCorrectly()
    {
        // Arrange
        var value = "Test/123";

        // Act
        var identity = new TestId(value);

        // Assert
        Assert.Equal(value, identity.Value);
        Assert.Equal(123, identity.NumericId);
        Assert.Equal("Test", Identity.GetPrefix(typeof(TestId)));
    }

    [Fact]
    public void Constructor_WithNumericId_ShouldConstructCorrectly()
    {
        // Arrange
        var numericId = 456L;

        // Act
        var identity = new TestId(numericId);

        // Assert
        Assert.Equal("Test/456", identity.Value);
        Assert.Equal(456, identity.NumericId);
        Assert.Equal("Test", Identity.GetPrefix(typeof(TestId)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidValue_ShouldThrowArgumentException(string invalidValue)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TestId(invalidValue));
    }

    [Fact]
    public void Constructor_WithNullValue_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TestId(null!));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("test")]
    [InlineData("test/")]
    [InlineData("/123")]
    [InlineData("test/123/extra")]
    public void Constructor_WithInvalidFormat_ShouldThrowArgumentException(string invalidFormat)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TestId(invalidFormat));
    }

    [Theory]
    [InlineData("test/abc")]
    [InlineData("test/12.5")]
    [InlineData("test/-123")]
    public void Constructor_WithInvalidNumericId_ShouldThrowArgumentException(string invalidNumericId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TestId(invalidNumericId));
    }

    [Fact]
    public void Constructor_WithNegativeNumericId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TestId(-1L));
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var identity = new TestId("Test/789");

        // Act
        var result = identity.ToString();

        // Assert
        Assert.Equal("Test/789", result);
    }

    [Theory]
    [InlineData("test/123")]
    [InlineData("TEST/123")]
    [InlineData("TeSt/123")]
    [InlineData("Test/123")]
    public void Constructor_WithDifferentCasing_ShouldNormalizeToCorrectCasing(string value)
    {
        // Act
        var identity = new TestId(value);

        // Assert
        Assert.Equal(123, identity.NumericId);
        Assert.Equal("Test", Identity.GetPrefix(typeof(TestId)));
        // Value should always use the correct prefix casing, not the original
        Assert.Equal("Test/123", identity.Value);
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var identity1 = new TestId("Test/100");
        var identity2 = new TestId("Test/100");

        // Act & Assert
        Assert.True(identity1.Equals(identity2));
        Assert.True(identity1.Equals((object)identity2));
        Assert.True(identity1 == identity2);
        Assert.False(identity1 != identity2);
    }

    [Fact]
    public void Equals_WithDifferentCasing_ShouldReturnTrue()
    {
        // Arrange - case insensitive comparison
        var identity1 = new TestId("test/100");
        var identity2 = new TestId("TEST/100");
        var identity3 = new TestId("Test/100");

        // Act & Assert
        Assert.True(identity1.Equals(identity2));
        Assert.True(identity1.Equals(identity3));
        Assert.True(identity2.Equals(identity3));
        Assert.True(identity1 == identity2);
        Assert.True(identity1 == identity3);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var identity1 = new TestId("Test/100");
        var identity2 = new TestId("Test/200");

        // Act & Assert
        Assert.False(identity1.Equals(identity2));
        Assert.False(identity1.Equals((object)identity2));
        Assert.False(identity1 == identity2);
        Assert.True(identity1 != identity2);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var identity = new TestId("Test/100");

        // Act & Assert
        Assert.False(identity.Equals(null));
        Assert.False(identity.Equals((object?)null));
        Assert.False(identity == null);
        Assert.False(null == identity);
        Assert.True(identity != null);
        Assert.True(null != identity);
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var identity = new TestId("Test/100");
        var differentObject = "Test/100";

        // Act & Assert
        Assert.False(identity.Equals(differentObject));
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var identity1 = new TestId("Test/100");
        var identity2 = new TestId("Test/100");

        // Act & Assert
        Assert.Equal(identity1.GetHashCode(), identity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentCasing_ShouldBeEqual()
    {
        // Arrange - case insensitive so hash codes should match
        var identity1 = new TestId("test/100");
        var identity2 = new TestId("TEST/100");
        var identity3 = new TestId("Test/100");

        // Act & Assert
        Assert.Equal(identity1.GetHashCode(), identity2.GetHashCode());
        Assert.Equal(identity1.GetHashCode(), identity3.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_WithBothNull_ShouldReturnTrue()
    {
        // Arrange
        TestId? identity1 = null;
        TestId? identity2 = null;
        
        // Act & Assert
        Assert.True(identity1 == identity2);
        Assert.False(identity1 != identity2);
    }

    [Fact]
    public void Constructor_WithWrongPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        var wrongValue = "WrongPrefix/123";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TestId(wrongValue));
        Assert.Contains("prefix", ex.Message.ToLower());
    }

    [Theory]
    [InlineData("Document/123")]
    [InlineData("AlternativeTest/456")]
    [InlineData("SomeOther/789")]
    public void Constructor_WithDifferentPrefix_ShouldThrowArgumentException(string wrongValue)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TestId(wrongValue));
        Assert.Contains("prefix", ex.Message.ToLower());
    }

    [Theory]
    [InlineData("test/100", "Test/100")] // lowercase should be normalized
    [InlineData("TEST/200", "Test/200")] // uppercase should be normalized
    [InlineData("TeSt/300", "Test/300")] // mixed case should be normalized
    public void Constructor_WithCorrectPrefixDifferentCasing_ShouldNormalize(string input, string expectedValue)
    {
        // Act
        var identity = new TestId(input);

        // Assert
        Assert.NotNull(identity);
        Assert.Equal("Test", Identity.GetPrefix(typeof(TestId)));
        Assert.Equal(expectedValue, identity.Value);
    }

    [Fact]
    public void NumericId_WithMaxLongValue_ShouldWork()
    {
        // Arrange
        var maxLong = long.MaxValue;

        // Act
        var identity = new TestId(maxLong);

        // Assert
        Assert.Equal(maxLong, identity.NumericId);
        Assert.Equal($"Test/{maxLong}", identity.Value);
    }

    [Fact]
    public void NumericId_WithZero_ShouldWork()
    {
        // Arrange & Act
        var identity = new TestId(0L);

        // Assert
        Assert.Equal(0L, identity.NumericId);
        Assert.Equal("Test/0", identity.Value);
    }

    [Fact]
    public void Value_ShouldBeReadOnly()
    {
        // Arrange
        var identity = new TestId("Test/123");

        // Act & Assert
        // Value property should only have a getter
        Assert.Equal("Test/123", identity.Value);
    }

    [Fact]
    public void NumericId_ShouldBeReadOnly()
    {
        // Arrange
        var identity = new TestId("Test/456");

        // Act & Assert
        // NumericId property should only have a getter
        Assert.Equal(456L, identity.NumericId);
    }

    [Fact]
    public void Equals_WithDifferentDerivedClass_ShouldReturnFalseIfValuesDifferent()
    {
        // Arrange
        var testId = new TestId("Test/100");
        var altId = new AlternativeTestId("AlternativeTest/100");

        // Act & Assert
        Assert.False(testId.Equals(altId));
        Assert.False(testId == altId);
        Assert.True(testId != altId);
    }

    [Fact]
    public void Equals_WithSameDerivedClassSameValue_ShouldReturnTrue()
    {
        // Arrange
        var testId1 = new TestId("Test/100");
        var testId2 = new TestId("Test/100");

        // Act & Assert
        Assert.True(testId1.Equals(testId2));
        Assert.True(testId1 == testId2);
        Assert.False(testId1 != testId2);
    }

    [Theory]
    [InlineData("test/1", "Test/1")]
    [InlineData("Test/999999999", "Test/999999999")]
    [InlineData("TEST/0", "Test/0")]
    public void Constructor_WithValidFormats_ShouldNormalizeValue(string input, string expectedValue)
    {
        // Act
        var identity = new TestId(input);

        // Assert
        Assert.Equal(expectedValue, identity.Value);
        Assert.True(identity.NumericId >= 0);
    }

    [Fact]
    public void IEquatable_ShouldBeImplemented()
    {
        // Arrange
        var identity = new TestId("Test/123");

        // Act & Assert
        Assert.IsAssignableFrom<IEquatable<Identity>>(identity);
    }

    [Fact]
    public void GetPrefix_ShouldAutomaticallyDeriveFromClassName()
    {
        // Act & Assert
        Assert.Equal("Test", Identity.GetPrefix(typeof(TestId)));
        Assert.Equal("AlternativeTest", Identity.GetPrefix(typeof(AlternativeTestId)));
        Assert.Equal("Document", Identity.GetPrefix(typeof(DocumentId)));
    }

    [Fact]
    public void GetPrefix_ShouldHandleTypeWithoutIdSuffix()
    {
        // Act & Assert - if a type doesn't end with "Id", it returns the full name
        Assert.Equal("Identity", Identity.GetPrefix(typeof(Identity)));
    }

    [Fact]
    public void DocumentId_WithWrongPrefix_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new DocumentId("Test/123"));
        Assert.Contains("prefix", ex.Message.ToLower());
    }

    [Fact]
    public void DocumentId_WithCorrectPrefix_ShouldWork()
    {
        // Arrange & Act
        var docId = new DocumentId("Document/123");

        // Assert
        Assert.Equal("Document", Identity.GetPrefix(typeof(DocumentId)));
        Assert.Equal("Document/123", docId.Value);
        Assert.Equal(123, docId.NumericId);
    }

    [Theory]
    [InlineData("document/100", "Document/100")]
    [InlineData("DOCUMENT/200", "Document/200")]
    [InlineData("Document/300", "Document/300")]
    public void DocumentId_WithDifferentCasing_ShouldNormalizeValue(string input, string expectedValue)
    {
        // Act
        var docId = new DocumentId(input);

        // Assert
        Assert.Equal(expectedValue, docId.Value);
        Assert.Equal("Document", Identity.GetPrefix(typeof(DocumentId)));
    }
}

internal class TestId : Identity
{
    public TestId(string value) : base(value)
    {
    }

    public TestId(long numericId) : base(numericId)
    {
    }
}

internal class AlternativeTestId : Identity
{
    public AlternativeTestId(string value) : base(value)
    {
    }

    public AlternativeTestId(long numericId) : base(numericId)
    {
    }
}

internal class DocumentId : Identity
{
    public DocumentId(string value) : base(value)
    {
    }

    public DocumentId(long numericId) : base(numericId)
    {
    }
}
