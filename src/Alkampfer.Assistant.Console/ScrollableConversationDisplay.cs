using Alkampfer.Assistant.Interfaces;
using Spectre.Console;

namespace Alkampfer.Assistant.Console;

/// <summary>
/// A console display for conversations with a truly sticky header and scrollable content area.
/// </summary>
public class ScrollableConversationDisplay : IDisposable
{
    private readonly ConversationDisplayInfo _displayInfo;
    private readonly StickyHeaderManager _headerManager;
    private bool _disposed;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollableConversationDisplay"/> class.
    /// </summary>
    public ScrollableConversationDisplay()
    {
        _displayInfo = new ConversationDisplayInfo();
        _headerManager = new StickyHeaderManager();
    }

    /// <summary>
    /// Gets the conversation display information.
    /// </summary>
    public ConversationDisplayInfo DisplayInfo => _displayInfo;

    /// <summary>
    /// Initializes the display - must be called before first use.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        AnsiConsole.Clear();
        System.Console.CursorVisible = false;
        RenderHeader();
        _isInitialized = true;
    }

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

        _headerManager.RenderHeader(panel);
    }

    /// <summary>
    /// Refreshes the entire display (header and content).
    /// </summary>
    public void Refresh()
    {
        RenderHeader();
        _headerManager.RenderContent();
    }

    /// <summary>
    /// Writes a line to the content area.
    /// </summary>
    /// <param name="markup">The markup to write.</param>
    public void WriteLine(string markup)
    {
        _headerManager.AddContentLine(markup);
        _headerManager.ScrollToBottom();
        _headerManager.RenderContent();
    }

    /// <summary>
    /// Writes multiple lines to the content area.
    /// </summary>
    /// <param name="lines">The lines to write.</param>
    public void WriteLines(params string[] lines)
    {
        foreach (var line in lines)
        {
            _headerManager.AddContentLine(line);
        }
        _headerManager.ScrollToBottom();
        _headerManager.RenderContent();
    }

    /// <summary>
    /// Clears the content area.
    /// </summary>
    public void ClearContent()
    {
        _headerManager.ClearContent();
        _headerManager.RenderContent();
    }

    /// <summary>
    /// Prompts the user for input at the bottom of the screen.
    /// </summary>
    /// <param name="prompt">The prompt text.</param>
    /// <returns>The user's input.</returns>
    public string PromptInput(string prompt)
    {
        // Move cursor to bottom for input
        var inputLine = System.Console.WindowHeight - 2;
        System.Console.SetCursorPosition(0, inputLine);

        // Clear the input line
        System.Console.Write(new string(' ', System.Console.WindowWidth - 1));
        System.Console.SetCursorPosition(0, inputLine);

        // Show cursor for input
        System.Console.CursorVisible = true;

        // Use Spectre.Console for styled input
        var result = AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .PromptStyle("white")
                .AllowEmpty());

        // Hide cursor after input
        System.Console.CursorVisible = false;

        // Clear the input line after getting input
        System.Console.SetCursorPosition(0, inputLine);
        System.Console.Write(new string(' ', System.Console.WindowWidth - 1));

        return result;
    }

    /// <summary>
    /// Shows a status message at the bottom of the screen while executing an async action.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="statusText">The status text to display.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The result of the action.</returns>
    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        var statusLine = System.Console.WindowHeight - 2;
        System.Console.SetCursorPosition(0, statusLine);

        // Create a simple status display
        var markup = new Markup($"[green]●[/] [dim]{statusText}[/]");
        AnsiConsole.Write(markup);

        try
        {
            return await action();
        }
        finally
        {
            // Clear status line
            System.Console.SetCursorPosition(0, statusLine);
            System.Console.Write(new string(' ', System.Console.WindowWidth - 1));
        }
    }

    /// <summary>
    /// Scrolls the content up.
    /// </summary>
    /// <param name="lines">Number of lines to scroll.</param>
    public void ScrollUp(int lines = 1)
    {
        if (_headerManager.ScrollUp(lines))
        {
            _headerManager.RenderContent();
        }
    }

    /// <summary>
    /// Scrolls the content down.
    /// </summary>
    /// <param name="lines">Number of lines to scroll.</param>
    public void ScrollDown(int lines = 1)
    {
        if (_headerManager.ScrollDown(lines))
        {
            _headerManager.RenderContent();
        }
    }

    /// <summary>
    /// Scrolls to the top of the content.
    /// </summary>
    public void ScrollToTop()
    {
        _headerManager.ScrollToTop();
        _headerManager.RenderContent();
    }

    /// <summary>
    /// Scrolls to the bottom of the content.
    /// </summary>
    public void ScrollToBottom()
    {
        _headerManager.ScrollToBottom();
        _headerManager.RenderContent();
    }

    /// <summary>
    /// Disposes the resources used by the scrollable conversation display.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        System.Console.CursorVisible = true;
        _displayInfo.IsConversationActive = false;
        _headerManager.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
