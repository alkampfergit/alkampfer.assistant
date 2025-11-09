using Alkampfer.Assistant.Interfaces;
using Spectre.Console;

namespace Alkampfer.Assistant.Console;

/// <summary>
/// A specialized console display for conversations with a sticky header showing real-time statistics.
/// </summary>
public class ConversationConsoleDisplay : IDisposable
{
    private readonly ConversationDisplayInfo _displayInfo;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationConsoleDisplay"/> class.
    /// </summary>
    public ConversationConsoleDisplay()
    {
        _displayInfo = new ConversationDisplayInfo();
    }

    /// <summary>
    /// Gets the conversation display information.
    /// </summary>
    public ConversationDisplayInfo DisplayInfo => _displayInfo;

    /// <summary>
    /// Updates the display from conversation statistics.
    /// </summary>
    /// <param name="conversation">The conversation to get statistics from.</param>
    public void UpdateFromConversation(IConversation conversation)
    {
        _displayInfo.IsConversationActive = true;
        _displayInfo.TotalInputTokens = conversation.Statistics.TotalInputTokens;
        _displayInfo.TotalOutputTokens = conversation.Statistics.TotalOutputTokens;
        _displayInfo.LastCallInputTokens = conversation.Statistics.LastCallInputTokens;
        _displayInfo.LastCallOutputTokens = conversation.Statistics.LastCallOutputTokens;
    }

    /// <summary>
    /// Renders the header with current conversation statistics.
    /// </summary>
    public void RenderHeader()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn(new TableColumn("[bold yellow]Status[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Total Tokens[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Last Call Tokens[/]").Centered());

        table.AddRow(
            _displayInfo.IsConversationActive ? "[green]Active[/]" : "[red]Inactive[/]",
            $"In: [cyan]{_displayInfo.TotalInputTokens:N0}[/] | Out: [cyan]{_displayInfo.TotalOutputTokens:N0}[/] | Total: [cyan]{_displayInfo.TotalTokens:N0}[/]",
            $"In: [cyan]{_displayInfo.LastCallInputTokens:N0}[/] | Out: [cyan]{_displayInfo.LastCallOutputTokens:N0}[/] | Total: [cyan]{_displayInfo.LastCallTotalTokens:N0}[/]");

        var panel = new Panel(table)
        {
            Header = new PanelHeader("[bold blue]Conversation Information[/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue)
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Clears the console and renders the header.
    /// </summary>
    public void ClearAndRenderHeader()
    {
        AnsiConsole.Clear();
        RenderHeader();
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes a line to the console.
    /// </summary>
    /// <param name="markup">The markup to write.</param>
    public void WriteLine(string markup)
    {
        AnsiConsole.MarkupLine(markup);
    }

    /// <summary>
    /// Writes markup to the console.
    /// </summary>
    /// <param name="markup">The markup to write.</param>
    public void Write(string markup)
    {
        AnsiConsole.Markup(markup);
    }

    /// <summary>
    /// Prompts the user for input.
    /// </summary>
    /// <param name="prompt">The prompt text.</param>
    /// <returns>The user's input.</returns>
    public string PromptInput(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .PromptStyle("white")
                .AllowEmpty());
    }

    /// <summary>
    /// Shows a status spinner while executing an async action.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="statusText">The status text to display.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The result of the action.</returns>
    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync(statusText, async ctx => await action());
    }

    /// <summary>
    /// Disposes the resources used by the conversation console display.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _displayInfo.IsConversationActive = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
