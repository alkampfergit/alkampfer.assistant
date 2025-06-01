using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly ICounterManager _counterManager;
    private readonly IdentityManager _manager;

    public IdentityManagerTests()
    {
        _counterManager = new InMemoryCounterManager();
        _manager = new IdentityManager(_counterManager);
    }
    
    [Fact]
    public void RegisterIdentityType_ValidType_RegistersSuccessfully()
    {
        // Arrange & Act & Assert - Should not throw
        _manager.RegisterIdentityType<TestUserId>();
    }
    
    [Fact]
    public void RegisterIdentityType_MultipleTypes_RegistersAllSuccessfully()
    {
        // Arrange & Act & Assert - Should not throw
        _manager.RegisterIdentityType<TestUserId>();
        _manager.RegisterIdentityType<TestOrderId>();
        _manager.RegisterIdentityType<TestProductId>();
    }
    
    [Fact]
    public void RegisterIdentityType_TypeWithoutLongConstructor_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _manager.RegisterIdentityType<InvalidTestId>());
        
        Assert.Contains("must have a constructor with a single long parameter", exception.Message);
    }
    
    [Fact]
    public void Parse_ValidRegisteredIdentity_ReturnsCorrectType()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = _manager.Parse("user/123");
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/123", result.Value);
        Assert.Equal(123, result.NumericId);
    }
    
    [Fact]
    public void Parse_MultipleRegisteredTypes_ReturnsCorrectTypeBasedOnPrefix()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        _manager.RegisterIdentityType<TestOrderId>();
        _manager.RegisterIdentityType<TestProductId>();
        
        // Act
        var userResult = _manager.Parse("user/456");
        var orderResult = _manager.Parse("order/789");
        var productResult = _manager.Parse("product/101");
        
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
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _manager.Parse("unknown/123"));
        
        Assert.Contains("Unknown identity prefix: 'unknown'", exception.Message);
    }
    
    [Fact]
    public void Parse_NullOrEmptyValue_ThrowsArgumentException()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var nullException = Assert.Throws<ArgumentException>(() => 
            _manager.Parse(null!));
        Assert.Contains("Identity value cannot be null or empty", nullException.Message);
        
        var emptyException = Assert.Throws<ArgumentException>(() => 
            _manager.Parse(""));
        Assert.Contains("Identity value cannot be null or empty", emptyException.Message);
        
        var whitespaceException = Assert.Throws<ArgumentException>(() => 
            _manager.Parse("   "));
        Assert.Contains("Identity value cannot be null or empty", whitespaceException.Message);
    }
    
    [Fact]
    public void Parse_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var noSlashException = Assert.Throws<ArgumentException>(() => 
            _manager.Parse("user123"));
        Assert.Contains("Invalid identity format. Expected 'prefix/numericId'", noSlashException.Message);
        
        var multipleSlashException = Assert.Throws<ArgumentException>(() => 
            _manager.Parse("user/123/extra"));
        Assert.Contains("Invalid identity format. Expected 'prefix/numericId'", multipleSlashException.Message);
    }
    
    [Fact]
    public void Parse_InvalidNumericId_ThrowsArgumentException()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var nonNumericException = Assert.Throws<ArgumentException>(() => 
            _manager.Parse("user/abc"));
        Assert.Contains("Invalid numeric id in identity 'user/abc'", nonNumericException.Message);
        
        var negativeException = Assert.Throws<ArgumentException>(() => 
            _manager.Parse("user/-123"));
        Assert.Contains("Numeric id must be non-negative in identity 'user/-123'", negativeException.Message);
    }
    
    [Fact]
    public void Parse_EmptyPrefix_ThrowsArgumentException()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _manager.Parse("/123"));
        Assert.Contains("Prefix cannot be null or empty in identity '/123'", exception.Message);
    }
    
    [Fact]
    public void RegisterIdentityType_SameTypeTwice_DoesNotThrow()
    {
        // Arrange & Act & Assert - Should not throw when registering the same type twice
        _manager.RegisterIdentityType<TestUserId>();
        _manager.RegisterIdentityType<TestUserId>(); // Should overwrite, not throw
        
        // Should still work correctly
        var result = _manager.Parse("user/123");
        Assert.IsType<TestUserId>(result);
    }
    
    [Fact]
    public void Parse_ValidZeroId_ReturnsCorrectIdentity()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = _manager.Parse("user/0");
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/0", result.Value);
        Assert.Equal(0, result.NumericId);
    }
    
    [Fact]
    public void Parse_LargeNumericId_ReturnsCorrectIdentity()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = _manager.Parse("user/9223372036854775807"); // long.MaxValue
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/9223372036854775807", result.Value);
        Assert.Equal(9223372036854775807, result.NumericId);
    }
    
    [Fact]
    public void Parse_WithLeadingZeros_ReturnsCorrectIdentity()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var result = _manager.Parse("user/00123");
        
        // Assert
        Assert.IsType<TestUserId>(result);
        Assert.Equal("user/00123", result.Value);
        Assert.Equal(123, result.NumericId); // Parsed value should be 123
    }
    
    [Fact]
    public async Task RegisterIdentityType_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var exceptions = new List<Exception>();
        
        // Act - Register types concurrently
        var tasks = new[]
        {
            Task.Run(() => 
            {
                try { _manager.RegisterIdentityType<TestUserId>(); }
                catch (Exception ex) { lock(exceptions) exceptions.Add(ex); }
            }),
            Task.Run(() => 
            {
                try { _manager.RegisterIdentityType<TestOrderId>(); }
                catch (Exception ex) { lock(exceptions) exceptions.Add(ex); }
            }),
            Task.Run(() => 
            {
                try { _manager.RegisterIdentityType<TestProductId>(); }
                catch (Exception ex) { lock(exceptions) exceptions.Add(ex); }
            })
        };
        
        await Task.WhenAll(tasks);
        
        // Assert - No exceptions should occur
        Assert.Empty(exceptions);
        
        // All types should be registered and parsing should work
        var userResult = _manager.Parse("user/1");
        var orderResult = _manager.Parse("order/2");
        var productResult = _manager.Parse("product/3");
        
        Assert.IsType<TestUserId>(userResult);
        Assert.IsType<TestOrderId>(orderResult);
        Assert.IsType<TestProductId>(productResult);
    }

    [Fact]
    public async Task GenerateNewAsync_ByType_ReturnsNewIdentityWithIncrementedId()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        
        // Act
        var first = await _manager.GenerateNewAsync<TestUserId>();
        var second = await _manager.GenerateNewAsync<TestUserId>();
        var third = await _manager.GenerateNewAsync<TestUserId>();
        
        // Assert
        Assert.IsType<TestUserId>(first);
        Assert.IsType<TestUserId>(second);
        Assert.IsType<TestUserId>(third);
        
        Assert.Equal("user/1", first.Value);
        Assert.Equal("user/2", second.Value);
        Assert.Equal("user/3", third.Value);
        
        Assert.Equal(1, first.NumericId);
        Assert.Equal(2, second.NumericId);
        Assert.Equal(3, third.NumericId);
    }

    [Fact]
    public async Task GenerateNewAsync_ByPrefix_ReturnsNewIdentityWithIncrementedId()
    {
        // Arrange
        _manager.RegisterIdentityType<TestOrderId>();
        
        // Act
        var first = await _manager.GenerateNewAsync("order");
        var second = await _manager.GenerateNewAsync("order");
        var third = await _manager.GenerateNewAsync("order");
        
        // Assert
        Assert.IsType<TestOrderId>(first);
        Assert.IsType<TestOrderId>(second);
        Assert.IsType<TestOrderId>(third);
        
        Assert.Equal("order/1", first.Value);
        Assert.Equal("order/2", second.Value);
        Assert.Equal("order/3", third.Value);
        
        Assert.Equal(1, first.NumericId);
        Assert.Equal(2, second.NumericId);
        Assert.Equal(3, third.NumericId);
    }

    [Fact]
    public async Task GenerateNewAsync_MultipleTypes_IndependentCounters()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        _manager.RegisterIdentityType<TestOrderId>();
        
        // Act
        var user1 = await _manager.GenerateNewAsync<TestUserId>();
        var order1 = await _manager.GenerateNewAsync<TestOrderId>();
        var user2 = await _manager.GenerateNewAsync<TestUserId>();
        var order2 = await _manager.GenerateNewAsync<TestOrderId>();
        
        // Assert
        Assert.Equal("user/1", user1.Value);
        Assert.Equal("order/1", order1.Value);
        Assert.Equal("user/2", user2.Value);
        Assert.Equal("order/2", order2.Value);
    }

    [Fact]
    public async Task GenerateNewAsync_UnregisteredType_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await _manager.GenerateNewAsync<TestUserId>());
    }

    [Fact]
    public async Task GenerateNewAsync_UnregisteredPrefix_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await _manager.GenerateNewAsync("unknown"));
        
        Assert.Contains("Unknown identity prefix: 'unknown'", exception.Message);
    }

    [Fact]
    public async Task GenerateNewAsync_NullOrEmptyPrefix_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _manager.GenerateNewAsync((string)null!));
        
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _manager.GenerateNewAsync(""));
        
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _manager.GenerateNewAsync("   "));
    }

    [Fact]
    public async Task GenerateNewAsync_ConcurrentGeneration_ThreadSafe()
    {
        // Arrange
        _manager.RegisterIdentityType<TestUserId>();
        var generatedIds = new List<TestUserId>();
        var lockObject = new object();
        
        // Act - Generate IDs concurrently
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var id = await _manager.GenerateNewAsync<TestUserId>();
                lock (lockObject)
                {
                    generatedIds.Add(id);
                }
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Assert - All IDs should be unique and have values from 1 to 10
        Assert.Equal(10, generatedIds.Count);
        var numericIds = generatedIds.Select(id => id.NumericId).OrderBy(x => x).ToArray();
        var expectedIds = Enumerable.Range(1, 10).Select(x => (long)x).ToArray();
        Assert.Equal(expectedIds, numericIds);
    }
}
