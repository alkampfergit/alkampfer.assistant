using Alkampfer.Assistant.Console;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Alkampfer.Assistant.Interfaces.Llm;
using OpenAI.Responses;
using Spectre.Console;

#pragma warning disable OPENAI001

namespace Alkampfer.Assistant.Playground.Examples;

/// <summary>
/// Example demonstrating the AzureOpenAiResponseLanguageModel with reasoning capabilities in a conversation loop.
/// This example shows how to use the Response API with different reasoning effort levels
/// for multi-turn conversations with reasoning.
/// Note: The Response API is stateless, so conversation history is managed manually.
/// </summary>
public class ReasoningModelExample : ExampleBase
{
    public override string Name => "Reasoning Conversation with Azure OpenAI";

    public override string Description => "Multi-turn conversation using AzureOpenAiResponseLanguageModel with reasoning capabilities";

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Load environment variables from .env file
        DotEnv.Load();

        // Check for required environment variables
        var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("AZURE_MODEL");

        if (string.IsNullOrWhiteSpace(azureEndpoint) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(model))
        {
            AnsiConsole.MarkupLine("[red]Error: Azure OpenAI configuration not found![/]");
            AnsiConsole.MarkupLine("[dim]Please set the following environment variables:[/]");
            AnsiConsole.MarkupLine("[dim]  - AZURE_ENDPOINT[/]");
            AnsiConsole.MarkupLine("[dim]  - OPENAI_API_KEY[/]");
            AnsiConsole.MarkupLine("[dim]  - AZURE_MODEL[/]");
            return;
        }

        try
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new Panel("[yellow]Reasoning Conversation with Azure OpenAI[/]")
                    .BorderColor(Color.Yellow)
                    .Padding(1, 0));

            AnsiConsole.WriteLine();

            // Prompt user to select reasoning effort level
            var reasoningLevel = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select [green]reasoning effort level[/]:")
                    .AddChoices("Low", "Medium", "High"));

            var effortLevel = reasoningLevel switch
            {
                "Low" => ResponseReasoningEffortLevel.Low,
                "Medium" => ResponseReasoningEffortLevel.Medium,
                "High" => ResponseReasoningEffortLevel.High,
                _ => ResponseReasoningEffortLevel.Low
            };

            // Create the Azure OpenAI Response language model with reasoning
            var languageModel = new AzureOpenAiResponseLanguageModel(
                azureEndpoint,
                apiKey,
                model,
                effortLevel);

            AnsiConsole.Clear();

            // Track conversation history and statistics
            var conversationHistory = new List<(string Role, string Message)>();
            var totalInputTokens = 0;
            var totalOutputTokens = 0;

            AnsiConsole.MarkupLine($"[green]Reasoning conversation started![/] [dim](Effort: {reasoningLevel})[/]");
            AnsiConsole.MarkupLine("[dim]Type 'exit' or 'quit' to end the conversation.[/]");
            AnsiConsole.MarkupLine("[dim]Note: This uses the Response API which is stateless - each message is independent.[/]");
            AnsiConsole.WriteLine();

            // Main conversation loop
            while (true)
            {
                // Get user input
                var userMessage = AnsiConsole.Prompt(
                    new TextPrompt<string>("[blue]You:[/]")
                        .PromptStyle("white")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    continue;
                }

                // Check for exit commands
                if (userMessage.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    userMessage.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // Add user message to history
                conversationHistory.Add((Role: "user", Message: userMessage));

                try
                {
                    // Generate response with reasoning
                    LanguageModelResponse response = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("green"))
                        .StartAsync("Thinking with reasoning...", async ctx =>
                            await languageModel.GenerateResponseAsync(userMessage, cancellationToken));

                    // Add assistant response to history
                    conversationHistory.Add((Role: "assistant", Message: response.Response));

                    // Update token statistics
                    totalInputTokens += response.Statistics.InputTokens;
                    totalOutputTokens += response.Statistics.OutputTokens;

                    // Display the assistant's response
                    AnsiConsole.MarkupLine($"[green]Assistant:[/] {response.Response}");

                    // Display token statistics after each response
                    AnsiConsole.MarkupLine($"[dim]Tokens - In: {response.Statistics.InputTokens:N0} | Out: {response.Statistics.OutputTokens:N0} | Total this call: {response.Statistics.InputTokens + response.Statistics.OutputTokens:N0}[/]");
                    AnsiConsole.WriteLine();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    AnsiConsole.WriteLine();
                }
            }

            // Show final statistics
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]Conversation ended.[/]");
            AnsiConsole.WriteLine();
            DisplayStatistics(totalInputTokens, totalOutputTokens, conversationHistory.Count / 2);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal error: {ex.Message}[/]");
            if (AnsiConsole.Confirm("Show full exception details?", false))
            {
                AnsiConsole.WriteException(ex);
            }
        }
    }

    private static void DisplayStatistics(int totalInputTokens, int totalOutputTokens, int messageCount)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]Metric[/]").Centered())
            .AddColumn(new TableColumn("[yellow]Value[/]").Centered());

        table.AddRow("Messages Sent", messageCount.ToString("N0"));
        table.AddRow("Input Tokens", totalInputTokens.ToString("N0"));
        table.AddRow("Output Tokens", totalOutputTokens.ToString("N0"));
        table.AddRow("Total Tokens", (totalInputTokens + totalOutputTokens).ToString("N0"));

        AnsiConsole.Write(
            new Panel(table)
                .Header("[yellow]Conversation Statistics[/]")
                .BorderColor(Color.Yellow));
    }
}
