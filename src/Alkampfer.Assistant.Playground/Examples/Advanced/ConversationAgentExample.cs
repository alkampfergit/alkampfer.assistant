using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.Llm;
using Spectre.Console;

namespace Alkampfer.Assistant.Playground.Examples.Advanced;

/// <summary>
/// Example demonstrating the ConversationAgent with Azure OpenAI.
/// This example shows how to use the ConversationContext and ConversationAgent
/// to maintain a conversation with a language model.
/// </summary>
public class ConversationAgentExample : ExampleBase
{
    public override string Name => "Conversation Agent with Azure OpenAI";

    public override string Category => "Advanced Examples";

    public override string Description => "Demonstrates using ConversationAgent with Azure OpenAI for multi-turn conversations";

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[yellow]Conversation Agent Example[/]");
        AnsiConsole.WriteLine();

        // Check for required environment variables
        var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var deploymentId = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_ID");

        if (string.IsNullOrWhiteSpace(azureEndpoint) || 
            string.IsNullOrWhiteSpace(apiKey) || 
            string.IsNullOrWhiteSpace(deploymentId))
        {
            AnsiConsole.MarkupLine("[red]Error: Azure OpenAI configuration not found![/]");
            AnsiConsole.MarkupLine("[dim]Please set the following environment variables:[/]");
            AnsiConsole.MarkupLine("[dim]  - AZURE_OPENAI_ENDPOINT[/]");
            AnsiConsole.MarkupLine("[dim]  - AZURE_OPENAI_API_KEY[/]");
            AnsiConsole.MarkupLine("[dim]  - AZURE_OPENAI_DEPLOYMENT_ID[/]");
            return;
        }

        try
        {
            // Create the Azure OpenAI language model
            var languageModel = new AzureOpenAiChatLanguageModel(
                azureEndpoint,
                apiKey,
                deploymentId);

            // Create the conversation agent
            var agent = new ConversationAgent(languageModel);

            // Create a new conversation
            var conversation = new Conversation();

            // Start the conversation context (using statement ensures it's cleaned up)
            using (ConversationContext.StartConversation(conversation))
            {
                AnsiConsole.MarkupLine("[green]Conversation started![/]");
                AnsiConsole.MarkupLine("[dim]Type 'exit' or 'quit' to end the conversation.[/]");
                AnsiConsole.WriteLine();

                // Optional: Add a system message to set the conversation context
                await conversation.AddMessageAsync(
                    Interfaces.MessageRole.System, 
                    "You are a helpful AI assistant. Be concise and friendly.", 
                    cancellationToken);

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

                    // Send the message and get a response
                    try
                    {
                        string response = await AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .SpinnerStyle(Style.Parse("green"))
                            .StartAsync("[dim]Thinking...[/]", async ctx =>
                            {
                                return await agent.SendMessageAsync(userMessage, cancellationToken);
                            });

                        // Display the assistant's response
                        AnsiConsole.MarkupLine($"[green]Assistant:[/] {response}");
                        AnsiConsole.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                        AnsiConsole.WriteLine();
                    }
                }

                // Display conversation statistics
                AnsiConsole.WriteLine();
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
