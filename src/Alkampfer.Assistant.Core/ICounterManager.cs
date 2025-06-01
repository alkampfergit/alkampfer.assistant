using System;

namespace Alkampfer.Assistant.Core;

public interface ICounterManager
{
    /// <summary>
    /// Generates a new counter value for the specified counter name.
    /// </summary>
    /// <param name="counterName">The name of the counter.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The next value of the counter.</returns>
    Task<long> GenerateNewCounterAsync(string counterName, CancellationToken cancellationToken = default);
}
