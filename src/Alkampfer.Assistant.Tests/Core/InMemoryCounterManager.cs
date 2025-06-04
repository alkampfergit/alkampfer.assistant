using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;

namespace Alkampfer.Assistant.Tests.Core;

public class InMemoryCounterManager : ICounterManager
{
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public Task<long> GenerateNewCounterAsync(string counterName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(counterName))
            throw new ArgumentException("Counter name cannot be null or empty", nameof(counterName));

        var newValue = _counters.AddOrUpdate(counterName, 1, (key, current) => current + 1);
        return Task.FromResult(newValue);
    }

    public Task InitSeedAsync(string counterName, long seed = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(counterName))
            throw new ArgumentException("Counter name cannot be null or empty", nameof(counterName));

        _counters.TryAdd(counterName, seed);
        return Task.CompletedTask;
    }
}
