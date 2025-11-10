namespace Alkampfer.Assistant.Interfaces.Llm;

/// <summary>
/// Represents the capabilities of a language model.
/// </summary>
public class LlmCapabilities
{
    /// <summary>
    /// Gets or sets a value indicating whether the language model 
    /// supports conversation/chat mode. With Conversation support
    /// we can pass the previous response id in the request without
    /// the need to manage conversation history manually.
    /// </summary>
    public bool SupportConversation { get; set; }
}
