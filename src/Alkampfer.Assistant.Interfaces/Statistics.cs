namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Statistics for conversation token usage.
/// </summary>
public class Statistics
{
    /// <summary>
    /// Total number of input tokens across all calls.
    /// </summary>
    public int TotalInputTokens { get; private set; }

    /// <summary>
    /// Total number of output tokens across all calls.
    /// </summary>
    public int TotalOutputTokens { get; private set; }

    /// <summary>
    /// Number of input tokens from the last call.
    /// </summary>
    public int LastCallInputTokens { get; private set; }

    /// <summary>
    /// Number of output tokens from the last call.
    /// </summary>
    public int LastCallOutputTokens { get; private set; }

    /// <summary>
    /// Updates the statistics with token counts from the last call.
    /// </summary>
    /// <param name="inputTokens">Number of input tokens in the last call.</param>
    /// <param name="outputTokens">Number of output tokens in the last call.</param>
    public void UpdateStats(int inputTokens, int outputTokens)
    {
        if (inputTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputTokens), "Input tokens cannot be negative.");
        }

        if (outputTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputTokens), "Output tokens cannot be negative.");
        }

        LastCallInputTokens = inputTokens;
        LastCallOutputTokens = outputTokens;
        TotalInputTokens += inputTokens;
        TotalOutputTokens += outputTokens;
    }
}
