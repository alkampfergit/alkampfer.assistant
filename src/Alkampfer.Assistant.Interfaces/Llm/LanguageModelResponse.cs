namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Represents the response from a language model including the generated text,
/// token usage statistics, and the original response object.
/// </summary>
public class LanguageModelResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageModelResponse"/> class.
    /// </summary>
    /// <param name="response">The text response from the language model.</param>
    /// <param name="statistics">Token usage statistics for the request.</param>
    /// <param name="originalResponse">The original response object from the underlying model.</param>
    public LanguageModelResponse(string response, LanguageModelStatistics statistics, object? originalResponse = null)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        OriginalResponse = originalResponse;
    }

    /// <summary>
    /// Gets the text response from the language model.
    /// </summary>
    public string Response { get; }

    /// <summary>
    /// Gets the token usage statistics for this response.
    /// </summary>
    public LanguageModelStatistics Statistics { get; }

    /// <summary>
    /// Gets the original response object from the concrete model implementation.
    /// This can be cast to the specific type used by the underlying provider (e.g., Azure OpenAI types).
    /// </summary>
    public object? OriginalResponse { get; }

    /// <summary>
    /// Returns the response text.
    /// </summary>
    /// <returns>The text response from the language model.</returns>
    public override string ToString()
    {
        return Response;
    }

    /// <summary>
    /// Implicitly converts a <see cref="LanguageModelResponse"/> to a string.
    /// </summary>
    /// <param name="response">The response to convert.</param>
    public static implicit operator string(LanguageModelResponse response)
    {
        return response?.Response ?? string.Empty;
    }
}
