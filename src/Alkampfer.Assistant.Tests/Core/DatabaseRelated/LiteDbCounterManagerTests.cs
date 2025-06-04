using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

public class LiteDbCounterManagerTests : CounterManagerTestsBase, IDisposable
{
    private readonly List<string> _tempDbPaths;

    public LiteDbCounterManagerTests()
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

    protected override ICounterManager CreateCounterManager()
    {
        return new LiteDbCounterManager($"Filename={CreateTempDbPath()}");
    }

    [Fact]
    public async Task GenerateNewCounterAsync_WithoutInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "uninitialized-counter";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => counterManager.GenerateNewCounterAsync(counterName));

        Assert.Contains("not initialized", exception.Message);
        Assert.Contains(counterName, exception.Message);
    }

    [Fact]
    public async Task GenerateNewCounterAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "test-counter";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await counterManager.InitSeedAsync(counterName);

        // Act & Assert
        // Both OperationCanceledException and TaskCanceledException are valid cancellation exceptions
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => counterManager.GenerateNewCounterAsync(counterName, cts.Token));
    }

    [Fact]
    public async Task InitSeedAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "test-counter";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Both OperationCanceledException and TaskCanceledException are valid cancellation exceptions
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => counterManager.InitSeedAsync(counterName, 0, cts.Token));
    }

    [Fact]
    public async Task SequentialAccess_ShouldMaintainConsistency()
    {
        // Arrange
        var counterManager = CreateCounterManager();
        const string counterName = "sequential-counter";
        const int numberOfOperations = 50;

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
}
