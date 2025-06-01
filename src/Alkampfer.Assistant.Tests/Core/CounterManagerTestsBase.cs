using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

public abstract class CounterManagerTestsBase
{
    protected abstract ICounterManager CreateCounterManager();

    [Fact]
    public async Task InitSeedAsync_WithValidCounterName_ShouldInitializeCounter()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "test-counter";
        const long seedValue = 100;

        // Act
        await counterManager.InitSeedAsync(counterName, seedValue);

        // Assert
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(seedValue + 1, firstValue);
    }

    [Fact]
    public async Task InitSeedAsync_WithDefaultSeed_ShouldInitializeCounterToZero()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "test-counter";

        // Act
        await counterManager.InitSeedAsync(counterName);

        // Assert
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(1, firstValue);
    }

    [Fact]
    public async Task InitSeedAsync_CalledMultipleTimes_ShouldNotOverwriteExistingCounter()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "test-counter";

        // Act
        await counterManager.InitSeedAsync(counterName, 10);
        await counterManager.InitSeedAsync(counterName, 20); // Should not overwrite

        // Assert
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(11, firstValue); // Should be 10 + 1, not 20 + 1
    }

    [Fact]
    public async Task GenerateNewCounterAsync_AfterInitialization_ShouldReturnIncrementedValues()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "test-counter";
        const long seedValue = 5;

        await counterManager.InitSeedAsync(counterName, seedValue);

        // Act & Assert
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(6, firstValue);

        var secondValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(7, secondValue);

        var thirdValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(8, thirdValue);
    }

    [Fact]
    public async Task GenerateNewCounterAsync_WithMultipleCounters_ShouldMaintainSeparateValues()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counter1 = "counter-1";
        const string counter2 = "counter-2";

        await counterManager.InitSeedAsync(counter1, 10);
        await counterManager.InitSeedAsync(counter2, 20);

        // Act
        var value1a = await counterManager.GenerateNewCounterAsync(counter1);
        var value2a = await counterManager.GenerateNewCounterAsync(counter2);
        var value1b = await counterManager.GenerateNewCounterAsync(counter1);
        var value2b = await counterManager.GenerateNewCounterAsync(counter2);

        // Assert
        Assert.Equal(11, value1a);
        Assert.Equal(21, value2a);
        Assert.Equal(12, value1b);
        Assert.Equal(22, value2b);
    }

    [Fact]
    public async Task CounterManager_WithNegativeSeed_ShouldWorkCorrectly()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "negative-seed-counter";
        const long negativeSeed = -10;

        // Act
        await counterManager.InitSeedAsync(counterName, negativeSeed);
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        var secondValue = await counterManager.GenerateNewCounterAsync(counterName);

        // Assert
        Assert.Equal(-9, firstValue);
        Assert.Equal(-8, secondValue);
    }

    [Fact]
    public async Task CounterManager_WithLargeSeed_ShouldWorkCorrectly()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "large-seed-counter";
        const long largeSeed = long.MaxValue - 2;

        // Act
        await counterManager.InitSeedAsync(counterName, largeSeed);
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);

        // Assert
        Assert.Equal(long.MaxValue - 1, firstValue);
    }
}
