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
    /// <returns>A <see cref="LanguageModelResponse"/> containing the generated response, statistics, and original response object.</returns>
    Task<LanguageModelResponse> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a response from a conversation request with optional previous conversation continuation.
    /// </summary>
    /// <param name="request">The LLM request containing conversation messages and optional previous conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="LanguageModelResponse"/> containing the generated response, statistics, and original response object.</returns>
    /// <exception cref="NotSupportedException">Thrown when PreviousConversationId is provided but not supported by the model.</exception>
    Task<LanguageModelResponse> GenerateResponseAsync(LlmRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the capabilities of the language model.
    /// </summary>
    /// <returns>A <see cref="LlmCapabilities"/> object describing the model's capabilities.</returns>
    LlmCapabilities GetCapability();
}
