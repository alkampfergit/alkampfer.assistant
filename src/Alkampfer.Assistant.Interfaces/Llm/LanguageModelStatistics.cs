namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Statistics for a single language model call.
/// Contains token usage information for the current request only.
/// </summary>
public class LanguageModelStatistics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageModelStatistics"/> class.
    /// </summary>
    /// <param name="inputTokens">Number of input tokens consumed in this call.</param>
    /// <param name="outputTokens">Number of output tokens generated in this call.</param>
    public LanguageModelStatistics(int inputTokens, int outputTokens)
    {
        if (inputTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputTokens), "Input tokens cannot be negative.");
        }

        if (outputTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputTokens), "Output tokens cannot be negative.");
        }

        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    /// <summary>
    /// Gets the number of input tokens consumed in this call.
    /// </summary>
    public int InputTokens { get; }

    /// <summary>
    /// Gets the number of output tokens generated in this call.
    /// </summary>
    public int OutputTokens { get; }

    /// <summary>
    /// Gets the total number of tokens (input + output) for this call.
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
}
