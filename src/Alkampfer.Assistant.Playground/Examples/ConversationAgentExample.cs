using Alkampfer.Assistant.Console;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Spectre.Console;

namespace Alkampfer.Assistant.Playground.Examples;

/// <summary>
/// Example demonstrating the ConversationAgent with Azure OpenAI.
/// This example shows how to use the ConversationContext and ConversationAgent
/// to maintain a conversation with a language model.
/// </summary>
public class ConversationAgentExample : ExampleBase
{
    public override string Name => "Conversation Agent with Azure OpenAI";

    public override string Description => "Demonstrates using ConversationAgent with Azure OpenAI for multi-turn conversations";

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Load environment variables from .env file
        DotEnv.Load();

        // Check for required environment variables (same as tests use)
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
            // Create the Azure OpenAI language model
            var languageModel = new AzureOpenAiChatLanguageModel(
                azureEndpoint,
                apiKey,
                model);

            // Create the conversation agent
            var agent = new ConversationAgent(languageModel);

            // Create a new conversation
            var conversation = new Conversation();

            // Create the console display with sticky header
            using var display = new ConversationConsoleDisplay();

            // Start the conversation context (using statement ensures it's cleaned up)
            using (ConversationContext.StartConversation(conversation))
            {
                // Initialize display and show header
                display.UpdateFromConversation(conversation);
                display.ClearAndRenderHeader();

                display.WriteLine("[green]Conversation started![/]");
                display.WriteLine("[dim]Type 'exit' or 'quit' to end the conversation.[/]");
                display.WriteLine(string.Empty);

                // Optional: Add a system message to set the conversation context
                await conversation.AddMessageAsync(
                    Interfaces.MessageRole.System,
                    "You are a helpful AI assistant. Be concise and friendly.",
                    cancellationToken);

                // Main conversation loop
                while (true)
                {
                    // Get user input
                    var userMessage = display.PromptInput("[blue]You:[/]");

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

                    // Send the message and get a response
                    try
                    {
                        string response = await display.ShowStatusAsync(
                            "[dim]Thinking...[/]",
                            async () => await agent.SendMessageAsync(userMessage, cancellationToken));

                        // Update the display with new statistics and re-render
                        display.UpdateFromConversation(conversation);
                        display.ClearAndRenderHeader();

                        // Display the assistant's response
                        display.WriteLine($"[green]Assistant:[/] {response}");
                        display.WriteLine(string.Empty);
                    }
                    catch (Exception ex)
                    {
                        display.WriteLine($"[red]Error: {ex.Message}[/]");
                        display.WriteLine(string.Empty);
                    }
                }

                // Display final statistics
                display.WriteLine(string.Empty);
                DisplayStatistics(conversation);
            }

            AnsiConsole.MarkupLine("[green]Conversation ended.[/]");
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

    private static void DisplayStatistics(Conversation conversation)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]Metric[/]").Centered())
            .AddColumn(new TableColumn("[yellow]Value[/]").Centered());

        table.AddRow("Input Tokens", conversation.Statistics.TotalInputTokens.ToString("N0"));
        table.AddRow("Output Tokens", conversation.Statistics.TotalOutputTokens.ToString("N0"));
        table.AddRow("Total Tokens", (conversation.Statistics.TotalInputTokens + conversation.Statistics.TotalOutputTokens).ToString("N0"));

        AnsiConsole.Write(
            new Panel(table)
                .Header("[yellow]Conversation Statistics[/]")
                .BorderColor(Color.Yellow));
    }
}
