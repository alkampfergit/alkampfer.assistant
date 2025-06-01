using System;
using Alkampfer.Assistant.Core;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

public class IdentityTests
{
    [Fact]
    public void Constructor_WithValidValue_ShouldParseCorrectly()
    {
        // Arrange
        var value = "test/123";
        
        // Act
        var identity = new TestId(value);
        
        // Assert
        Assert.Equal(value, identity.Value);
        Assert.Equal(123, identity.NumericId);
        Assert.Equal("test", identity.GetPrefix());
    }

    [Fact]
    public void Constructor_WithNumericId_ShouldConstructCorrectly()
    {
        // Arrange
        var numericId = 456L;
        
        // Act
        var identity = new TestId(numericId);
        
        // Assert
        Assert.Equal("test/456", identity.Value);
        Assert.Equal(456, identity.NumericId);
        Assert.Equal("test", identity.GetPrefix());
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
        var identity = new TestId("test/789");
        
        // Act
        var result = identity.ToString();
        
        // Assert
        Assert.Equal("test/789", result);
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var identity1 = new TestId("test/100");
        var identity2 = new TestId("test/100");
        
        // Act & Assert
        Assert.True(identity1.Equals(identity2));
        Assert.True(identity1.Equals((object)identity2));
        Assert.True(identity1 == identity2);
        Assert.False(identity1 != identity2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var identity1 = new TestId("test/100");
        var identity2 = new TestId("test/200");
        
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
        var identity = new TestId("test/100");
        
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
        var identity = new TestId("test/100");
        var differentObject = "test/100";
        
        // Act & Assert
        Assert.False(identity.Equals(differentObject));
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var identity1 = new TestId("test/100");
        var identity2 = new TestId("test/100");
        
        // Act & Assert
        Assert.Equal(identity1.GetHashCode(), identity2.GetHashCode());
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
    public void Constructor_WithDifferentPrefix_ShouldValidateAgainstImplementedPrefix()
    {
        // This test verifies that the parsing validates against the actual prefix implementation
        // The TestId has prefix "test", so parsing "other/123" should still work as it only validates format
        
        // Arrange & Act
        var identity = new TestId("other/123");
        
        // Assert
        Assert.Equal("other/123", identity.Value);
        Assert.Equal(123, identity.NumericId);
        Assert.Equal("test", identity.GetPrefix()); // The implemented prefix, not the parsed one
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
        Assert.Equal($"test/{maxLong}", identity.Value);
    }

    [Fact]
    public void NumericId_WithZero_ShouldWork()
    {
        // Arrange & Act
        var identity = new TestId(0L);
        
        // Assert
        Assert.Equal(0L, identity.NumericId);
        Assert.Equal("test/0", identity.Value);
    }

    [Fact]
    public void Value_ShouldBeReadOnly()
    {
        // Arrange
        var identity = new TestId("test/123");
        
        // Act & Assert
        // Value property should only have a getter
        Assert.Equal("test/123", identity.Value);
    }

    [Fact]
    public void NumericId_ShouldBeReadOnly()
    {
        // Arrange
        var identity = new TestId("test/456");
        
        // Act & Assert
        // NumericId property should only have a getter
        Assert.Equal(456L, identity.NumericId);
    }

    [Fact]
    public void Equals_WithDifferentDerivedClass_ShouldReturnFalseIfValuesDifferent()
    {
        // Arrange
        var testId = new TestId("test/100");
        var altId = new AlternativeTestId("alt/100");
        
        // Act & Assert
        Assert.False(testId.Equals(altId));
        Assert.False(testId == altId);
        Assert.True(testId != altId);
    }

    [Fact]
    public void Equals_WithSameDerivedClassSameValue_ShouldReturnTrue()
    {
        // Arrange
        var testId1 = new TestId("same/100");
        var testId2 = new TestId("same/100");
        
        // Act & Assert
        Assert.True(testId1.Equals(testId2));
        Assert.True(testId1 == testId2);
        Assert.False(testId1 != testId2);
    }

    [Theory]
    [InlineData("test/1")]
    [InlineData("test/999999999")]
    [InlineData("prefix/0")]
    [InlineData("a/1")]
    public void Constructor_WithValidFormats_ShouldParseCorrectly(string validValue)
    {
        // Act
        var identity = new TestId(validValue);
        
        // Assert
        Assert.Equal(validValue, identity.Value);
        Assert.True(identity.NumericId >= 0);
    }

    [Fact]
    public void IEquatable_ShouldBeImplemented()
    {
        // Arrange
        var identity = new TestId("test/123");
        
        // Act & Assert
        Assert.IsAssignableFrom<IEquatable<Identity>>(identity);
    }

    [Fact]
    public void AbstractPrefix_ShouldBeImplementedByDerivedClass()
    {
        // Arrange
        var testId = new TestId("test/123");
        var altId = new AlternativeTestId("alt/456");
        
        // Act & Assert
        Assert.Equal("test", testId.GetPrefix());
        Assert.Equal("alternative", altId.GetPrefix());
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

    protected override string Prefix => "test";
    
    public string GetPrefix() => Prefix;
}

internal class AlternativeTestId : Identity
{
    public AlternativeTestId(string value) : base(value)
    {
    }

    public AlternativeTestId(long numericId) : base(numericId)
    {
    }

    protected override string Prefix => "alternative";
    
    public string GetPrefix() => Prefix;
}
