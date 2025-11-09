namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Provides token counting functionality for different language models.
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Counts the number of tokens in the provided text for the specified model.
    /// </summary>
    /// <param name="modelIdentifier">The identifier of the model (e.g., "gpt-4o", "gpt-4o-mini").</param>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>The number of tokens in the text for the specified model.</returns>
    int CountTokens(string modelIdentifier, string text);
}
