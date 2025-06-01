using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

public class InMemoryCounterManagerTests : CounterManagerTestsBase
{
    protected override ICounterManager CreateCounterManager()
    {
        return new InMemoryCounterManager();
    }

    [Fact]
    public async Task GenerateNewCounterAsync_WithoutInitialization_ShouldIncrementFromOne()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "uninitialized-counter";

        // Act
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        var secondValue = await counterManager.GenerateNewCounterAsync(counterName);

        // Assert
        Assert.Equal(1, firstValue);
        Assert.Equal(2, secondValue);
    }

    [Fact]
    public async Task GenerateNewCounterAsync_WithNullOrEmptyCounterName_ShouldThrowArgumentException()
    {
        // Arrange
        var counterManager = CreateCounterManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.GenerateNewCounterAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.GenerateNewCounterAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.GenerateNewCounterAsync("   "));
    }

    [Fact]
    public async Task InitSeedAsync_WithNullOrEmptyCounterName_ShouldThrowArgumentException()
    {
        // Arrange
        var counterManager = CreateCounterManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.InitSeedAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.InitSeedAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.InitSeedAsync("   "));
    }

    [Fact]
    public async Task WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "test-counter";
        using var cts = new CancellationTokenSource();

        // Act & Assert - InMemoryCounterManager doesn't actually check cancellation tokens
        // but should accept them without throwing
        await counterManager.InitSeedAsync(counterName, 0, cts.Token);
        var result = await counterManager.GenerateNewCounterAsync(counterName, cts.Token);
        
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldMaintainConsistency()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "concurrent-counter";
        const int numberOfTasks = 10;
        const int operationsPerTask = 10;

        await counterManager.InitSeedAsync(counterName, 0);

        // Act
        var tasks = new Task<long[]>[numberOfTasks];
        for (int i = 0; i < numberOfTasks; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var results = new long[operationsPerTask];
                for (int j = 0; j < operationsPerTask; j++)
                {
                    results[j] = await counterManager.GenerateNewCounterAsync(counterName);
                }
                return results;
            });
        }

        var allResults = await Task.WhenAll(tasks);

        // Assert
        var allValues = new List<long>();
        foreach (var taskResults in allResults)
        {
            allValues.AddRange(taskResults);
        }

        // All values should be unique (no duplicates due to race conditions)
        Assert.Equal(allValues.Count, allValues.Distinct().Count());

        // Values should be in the expected range
        Assert.All(allValues, value => Assert.InRange(value, 1, numberOfTasks * operationsPerTask));

        // The maximum value should be exactly the total number of operations
        Assert.Equal(numberOfTasks * operationsPerTask, allValues.Max());
    }
}
