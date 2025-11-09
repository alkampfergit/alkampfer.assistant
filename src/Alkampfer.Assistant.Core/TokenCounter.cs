using Alkampfer.Assistant.Interfaces;
using Microsoft.ML.Tokenizers;

namespace Alkampfer.Assistant.Core;

/// <summary>
/// Implements token counting using Microsoft.ML.Tokenizers for GPT models.
/// </summary>
public class TokenCounter : ITokenCounter
{
    private static readonly Lazy<Tokenizer> _gptTokenizer = new(() =>
    {
        // TikToken tokenizer for GPT models (o200k_base encoding used by GPT-4o and GPT-4o-mini)
        return TiktokenTokenizer.CreateForModel("gpt-4o");
    });

    /// <inheritdoc />
    public int CountTokens(string modelIdentifier, string text)
    {
        ArgumentNullException.ThrowIfNull(modelIdentifier);
        ArgumentNullException.ThrowIfNull(text);

        // GPT-4o, GPT-4o-mini, GPT-5-mini and other GPT models use the o200k_base encoding
        // Other GPT models can be added as needed
        if (modelIdentifier.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase) ||
            modelIdentifier.StartsWith("gpt-4o", StringComparison.OrdinalIgnoreCase) ||
            modelIdentifier.StartsWith("gpt-4", StringComparison.OrdinalIgnoreCase) ||
            modelIdentifier.StartsWith("gpt-3.5", StringComparison.OrdinalIgnoreCase))
        {
            var tokenizer = _gptTokenizer.Value;
            var tokens = tokenizer.CountTokens(text);
            return tokens;
        }

        throw new NotSupportedException($"Model '{modelIdentifier}' is not supported for token counting.");
    }
}
