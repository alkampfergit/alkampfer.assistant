using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

// Test identity classes for testing purposes
public class TestUserId : Identity
{
    protected override string Prefix => "user";
    
    public TestUserId(string value) : base(value) { }
    public TestUserId(long numericId) : base(numericId) { }
}

public class TestOrderId : Identity
{
    protected override string Prefix => "order";
    
    public TestOrderId(string value) : base(value) { }
    public TestOrderId(long numericId) : base(numericId) { }
}

public class TestProductId : Identity
{
    protected override string Prefix => "product";
    
    public TestProductId(string value) : base(value) { }
    public TestProductId(long numericId) : base(numericId) { }
}

// Invalid test identity class without proper constructors
public class InvalidTestId : Identity
{
    protected override string Prefix => "invalid";
    
    // Missing constructor with long parameter
    public InvalidTestId(string value) : base(value) { }
}

public class IdentityManagerTests
{
    [Fact]
    public void RegisterIdentityType_ValidType_RegistersSuccessfully()
    {
        // Arrange
        var manager = new IdentityManager();
        
        // Act & Assert - Should not throw
        manager.RegisterIdentityType<TestUserId>();
    }
    
    [Fact]
    public void RegisterIdentityType_MultipleTypes_RegistersAllSuccessfully()
    {
        // Arrange
        var manager = new IdentityManager();
        
        // Act & Assert - Should not throw
        manager.RegisterIdentityType<TestUserId>();
        manager.RegisterIdentityType<TestOrderId>();
        manager.RegisterIdentityType<TestProductId>();
    }
    
    [Fact]
    public void RegisterIdentityType_TypeWithoutLongConstructor_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new IdentityManager();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            manager.RegisterIdentityType<InvalidTestId>());
        
        Assert.Contains("must have a constructor with a single long parameter", exception.Message);
    }
    
    [Fact]
    public void Parse_ValidRegisteredIdentity_ReturnsCorrectType()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = manager.Parse("user/123");
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/123", result.Value);
        Assert.Equal(123, result.NumericId);
    }
    
    [Fact]
    public void Parse_MultipleRegisteredTypes_ReturnsCorrectTypeBasedOnPrefix()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        manager.RegisterIdentityType<TestOrderId>();
        manager.RegisterIdentityType<TestProductId>();
        
        // Act
        var userResult = manager.Parse("user/456");
        var orderResult = manager.Parse("order/789");
        var productResult = manager.Parse("product/101");
        
        // Assert
        Assert.IsType<TestUserId>(userResult);
        Assert.Equal("user/456", userResult.Value);
        Assert.Equal(456, userResult.NumericId);
        
        Assert.IsType<TestOrderId>(orderResult);
        Assert.Equal("order/789", orderResult.Value);
        Assert.Equal(789, orderResult.NumericId);
        
        Assert.IsType<TestProductId>(productResult);
        Assert.Equal("product/101", productResult.Value);
        Assert.Equal(101, productResult.NumericId);
    }
    
    [Fact]
    public void Parse_UnregisteredPrefix_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            manager.Parse("unknown/123"));
        
        Assert.Contains("Unknown identity prefix: 'unknown'", exception.Message);
    }
    
    [Fact]
    public void Parse_NullOrEmptyValue_ThrowsArgumentException()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var nullException = Assert.Throws<ArgumentException>(() => 
            manager.Parse(null!));
        Assert.Contains("Identity value cannot be null or empty", nullException.Message);
        
        var emptyException = Assert.Throws<ArgumentException>(() => 
            manager.Parse(""));
        Assert.Contains("Identity value cannot be null or empty", emptyException.Message);
        
        var whitespaceException = Assert.Throws<ArgumentException>(() => 
            manager.Parse("   "));
        Assert.Contains("Identity value cannot be null or empty", whitespaceException.Message);
    }
    
    [Fact]
    public void Parse_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var noSlashException = Assert.Throws<ArgumentException>(() => 
            manager.Parse("user123"));
        Assert.Contains("Invalid identity format. Expected 'prefix/numericId'", noSlashException.Message);
        
        var multipleSlashException = Assert.Throws<ArgumentException>(() => 
            manager.Parse("user/123/extra"));
        Assert.Contains("Invalid identity format. Expected 'prefix/numericId'", multipleSlashException.Message);
    }
    
    [Fact]
    public void Parse_InvalidNumericId_ThrowsArgumentException()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var nonNumericException = Assert.Throws<ArgumentException>(() => 
            manager.Parse("user/abc"));
        Assert.Contains("Invalid numeric id in identity 'user/abc'", nonNumericException.Message);
        
        var negativeException = Assert.Throws<ArgumentException>(() => 
            manager.Parse("user/-123"));
        Assert.Contains("Numeric id must be non-negative in identity 'user/-123'", negativeException.Message);
    }
    
    [Fact]
    public void Parse_EmptyPrefix_ThrowsArgumentException()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            manager.Parse("/123"));
        Assert.Contains("Prefix cannot be null or empty in identity '/123'", exception.Message);
    }
    
    [Fact]
    public void RegisterIdentityType_SameTypeTwice_DoesNotThrow()
    {
        // Arrange
        var manager = new IdentityManager();
        
        // Act & Assert - Should not throw when registering the same type twice
        manager.RegisterIdentityType<TestUserId>();
        manager.RegisterIdentityType<TestUserId>(); // Should overwrite, not throw
        
        // Should still work correctly
        var result = manager.Parse("user/123");
        Assert.IsType<TestUserId>(result);
    }
    
    [Fact]
    public void Parse_ValidZeroId_ReturnsCorrectIdentity()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = manager.Parse("user/0");
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/0", result.Value);
        Assert.Equal(0, result.NumericId);
    }
    
    [Fact]
    public void Parse_LargeNumericId_ReturnsCorrectIdentity()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = manager.Parse("user/9223372036854775807"); // long.MaxValue
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/9223372036854775807", result.Value);
        Assert.Equal(9223372036854775807, result.NumericId);
    }
    
    [Fact]
    public void Parse_WithLeadingZeros_ReturnsCorrectIdentity()
    {
        // Arrange
        var manager = new IdentityManager();
        manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = manager.Parse("user/00123");
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/00123", result.Value);
        Assert.Equal(123, result.NumericId); // Parsed value should be 123
    }
    
    [Fact]
    public async Task RegisterIdentityType_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var manager = new IdentityManager();
        var exceptions = new List<Exception>();
        
        // Act - Register types concurrently
        var tasks = new[]
        {
            Task.Run(() => 
            {
                try { manager.RegisterIdentityType<TestUserId>(); }
                catch (Exception ex) { lock(exceptions) exceptions.Add(ex); }
            }),
            Task.Run(() => 
            {
                try { manager.RegisterIdentityType<TestOrderId>(); }
                catch (Exception ex) { lock(exceptions) exceptions.Add(ex); }
            }),
            Task.Run(() => 
            {
                try { manager.RegisterIdentityType<TestProductId>(); }
                catch (Exception ex) { lock(exceptions) exceptions.Add(ex); }
            })
        };
        
        await Task.WhenAll(tasks);
        
        // Assert - No exceptions should occur
        Assert.Empty(exceptions);
        
        // All types should be registered and parsing should work
        var userResult = manager.Parse("user/1");
        var orderResult = manager.Parse("order/2");
        var productResult = manager.Parse("product/3");
        
        Assert.IsType<TestUserId>(userResult);
        Assert.IsType<TestOrderId>(orderResult);
        Assert.IsType<TestProductId>(productResult);
    }
}
