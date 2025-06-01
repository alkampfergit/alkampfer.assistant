using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core;

public class CounterManagerTests : IDisposable
{
    private readonly List<string> _tempDbPaths;

    public CounterManagerTests()
    {
        _tempDbPaths = new List<string>();
    }

    public void Dispose()
    {
        foreach (var dbPath in _tempDbPaths)
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private string CreateTempDbPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _tempDbPaths.Add(path);
        return path;
    }

    public static TheoryData<Func<CounterManagerTests, ICounterManager>> CounterManagerFactories =>
        new()
        {
            test => new InMemoryCounterManager(),
            test => new LiteDbCounterManager($"Filename={test.CreateTempDbPath()}")
        };

    public static TheoryData<Func<CounterManagerTests, ICounterManager>> LiteDbCounterManagerFactory =>
        new()
        {
            test => new LiteDbCounterManager($"Filename={test.CreateTempDbPath()}")
        };

    [Theory]
    [MemberData(nameof(CounterManagerFactories))]
    public async Task InitSeedAsync_WithValidCounterName_ShouldInitializeCounter(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
        const string counterName = "test-counter";
        const long seedValue = 100;

        // Act
        await counterManager.InitSeedAsync(counterName, seedValue);

        // Assert
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(seedValue + 1, firstValue);
    }

    [Theory]
    [MemberData(nameof(CounterManagerFactories))]
    public async Task InitSeedAsync_WithDefaultSeed_ShouldInitializeCounterToZero(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
        const string counterName = "test-counter";

        // Act
        await counterManager.InitSeedAsync(counterName);

        // Assert
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(1, firstValue);
    }

    [Theory]
    [MemberData(nameof(CounterManagerFactories))]
    public async Task InitSeedAsync_CalledMultipleTimes_ShouldNotOverwriteExistingCounter(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
        const string counterName = "test-counter";

        // Act
        await counterManager.InitSeedAsync(counterName, 10);
        await counterManager.InitSeedAsync(counterName, 20); // Should not overwrite

        // Assert
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        Assert.Equal(11, firstValue); // Should be 10 + 1, not 20 + 1
    }

    [Theory]
    [MemberData(nameof(CounterManagerFactories))]
    public async Task GenerateNewCounterAsync_AfterInitialization_ShouldReturnIncrementedValues(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
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

    [Theory]
    [MemberData(nameof(CounterManagerFactories))]
    public async Task GenerateNewCounterAsync_WithMultipleCounters_ShouldMaintainSeparateValues(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
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

    [Theory]
    [MemberData(nameof(LiteDbCounterManagerFactory))]
    public async Task GenerateNewCounterAsync_WithoutInitialization_ShouldThrowInvalidOperationException(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
        const string counterName = "uninitialized-counter";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => counterManager.GenerateNewCounterAsync(counterName));

        Assert.Contains("not initialized", exception.Message);
        Assert.Contains(counterName, exception.Message);
    }

    [Fact]
    public async Task InMemoryCounterManager_GenerateNewCounterAsync_WithoutInitialization_ShouldIncrementFromOne()
    {
        // Arrange
        var counterManager = new InMemoryCounterManager();
        const string counterName = "uninitialized-counter";

        // Act
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);
        var secondValue = await counterManager.GenerateNewCounterAsync(counterName);

        // Assert
        Assert.Equal(1, firstValue);
        Assert.Equal(2, secondValue);
    }

    [Theory]
    [MemberData(nameof(LiteDbCounterManagerFactory))]
    public async Task LiteDbCounterManager_GenerateNewCounterAsync_WithCancellationToken_ShouldRespectCancellation(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
        const string counterName = "test-counter";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await counterManager.InitSeedAsync(counterName);

        // Act & Assert
        // Both OperationCanceledException and TaskCanceledException are valid cancellation exceptions
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => counterManager.GenerateNewCounterAsync(counterName, cts.Token));
    }

    [Theory]
    [MemberData(nameof(LiteDbCounterManagerFactory))]
    public async Task LiteDbCounterManager_InitSeedAsync_WithCancellationToken_ShouldRespectCancellation(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
        const string counterName = "test-counter";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Both OperationCanceledException and TaskCanceledException are valid cancellation exceptions
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => counterManager.InitSeedAsync(counterName, 0, cts.Token));
    }

    [Fact]
    public async Task InMemoryCounterManager_WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        var counterManager = new InMemoryCounterManager();
        const string counterName = "test-counter";
        using var cts = new CancellationTokenSource();

        // Act & Assert - InMemoryCounterManager doesn't actually check cancellation tokens
        // but should accept them without throwing
        await counterManager.InitSeedAsync(counterName, 0, cts.Token);
        var result = await counterManager.GenerateNewCounterAsync(counterName, cts.Token);
        
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task InMemoryCounterManager_GenerateNewCounterAsync_WithNullOrEmptyCounterName_ShouldThrowArgumentException()
    {
        // Arrange
        var counterManager = new InMemoryCounterManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.GenerateNewCounterAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.GenerateNewCounterAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.GenerateNewCounterAsync("   "));
    }

    [Fact]
    public async Task InMemoryCounterManager_InitSeedAsync_WithNullOrEmptyCounterName_ShouldThrowArgumentException()
    {
        // Arrange
        var counterManager = new InMemoryCounterManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.InitSeedAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.InitSeedAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => counterManager.InitSeedAsync("   "));
    }

    [Fact]
    public async Task InMemoryCounterManager_ConcurrentAccess_ShouldMaintainConsistency()
    {
        // Arrange
        var counterManager = new InMemoryCounterManager();
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

    [Fact]
    public async Task LiteDbCounterManager_SequentialAccess_ShouldMaintainConsistency()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_sequential_{Guid.NewGuid()}.db");
        var counterManager = new LiteDbCounterManager($"Filename={dbPath}");
        const string counterName = "sequential-counter";
        const int numberOfOperations = 50;

        try
        {
            await counterManager.InitSeedAsync(counterName, 0);

            // Act - Sequential operations to avoid LiteDb concurrency issues
            var results = new List<long>();
            for (int i = 0; i < numberOfOperations; i++)
            {
                var value = await counterManager.GenerateNewCounterAsync(counterName);
                results.Add(value);
            }

            // Assert
            Assert.Equal(numberOfOperations, results.Count);
            var expected = Enumerable.Range(1, numberOfOperations).Select(x => (long)x);
            Assert.Equal(expected, results.OrderBy(x => x));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CounterManagerFactories))]
    public async Task CounterManager_WithNegativeSeed_ShouldWorkCorrectly(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
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

    [Theory]
    [MemberData(nameof(CounterManagerFactories))]
    public async Task CounterManager_WithLargeSeed_ShouldWorkCorrectly(Func<CounterManagerTests, ICounterManager> factory)
    {
        // Arrange
        var counterManager = factory(this);
        const string counterName = "large-seed-counter";
        const long largeSeed = long.MaxValue - 2;

        // Act
        await counterManager.InitSeedAsync(counterName, largeSeed);
        var firstValue = await counterManager.GenerateNewCounterAsync(counterName);

        // Assert
        Assert.Equal(long.MaxValue - 1, firstValue);
    }
}
