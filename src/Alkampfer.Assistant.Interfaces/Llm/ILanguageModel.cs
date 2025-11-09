namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Represents a language model that can generate responses from text prompts.
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// Generates a response from a text prompt.
    /// </summary>
    /// <param name="prompt">The input prompt to send to the language model.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The generated response from the language model.</returns>
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
