using Spectre.Console;

namespace Alkampfer.Assistant.Console;

/// <summary>
/// Provides a console interface with a sticky header that displays conversation information.
/// </summary>
public class StickyHeaderConsole : IDisposable
{
    private readonly ConversationDisplayInfo _displayInfo;
    private readonly Layout _layout;
    private readonly Panel _headerPanel;
    private readonly Panel _contentPanel;
    private readonly List<string> _contentLines = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StickyHeaderConsole"/> class.
    /// </summary>
    public StickyHeaderConsole()
    {
        _displayInfo = new ConversationDisplayInfo();

        // Create header panel
        _headerPanel = CreateHeaderPanel();

        // Create content panel
        _contentPanel = new Panel(string.Empty)
        {
            Border = BoxBorder.None,
            Padding = new Padding(0, 0, 0, 0)
        };

        // Create layout with header and content
        _layout = new Layout("Root")
            .SplitRows(
                new Layout("Header")
                    .Size(6)
                    .Update(_headerPanel),
                new Layout("Content")
                    .Update(_contentPanel));
    }

    /// <summary>
    /// Gets the conversation display information.
    /// </summary>
    public ConversationDisplayInfo DisplayInfo
    {
        get
        {
            lock (_lock)
            {
                return _displayInfo;
            }
        }
    }

    /// <summary>
    /// Updates the display information and refreshes the header.
    /// </summary>
    /// <param name="updateAction">Action to update the display info.</param>
    public void UpdateDisplayInfo(Action<ConversationDisplayInfo> updateAction)
    {
        lock (_lock)
        {
            updateAction(_displayInfo);
            UpdateHeader();
        }
    }

    /// <summary>
    /// Writes a line to the content area.
    /// </summary>
    /// <param name="markup">The markup text to write.</param>
    public void WriteLine(string markup)
    {
        lock (_lock)
        {
            _contentLines.Add(markup);
            UpdateContent();
        }
    }

    /// <summary>
    /// Writes markup to the content area without a newline.
    /// </summary>
    /// <param name="markup">The markup text to write.</param>
    public void Write(string markup)
    {
        lock (_lock)
        {
            if (_contentLines.Count == 0)
            {
                _contentLines.Add(markup);
            }
            else
            {
                _contentLines[^1] += markup;
            }
            UpdateContent();
        }
    }

    /// <summary>
    /// Clears the content area.
    /// </summary>
    public void ClearContent()
    {
        lock (_lock)
        {
            _contentLines.Clear();
            UpdateContent();
        }
    }

    /// <summary>
    /// Renders the entire layout to the console.
    /// </summary>
    public void Render()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(_layout);
    }

    /// <summary>
    /// Starts a live display session with the sticky header.
    /// </summary>
    /// <param name="action">The action to execute within the live display context.</param>
    public async Task StartLiveDisplayAsync(Func<StickyHeaderConsole, Task> action)
    {
        await AnsiConsole.Live(_layout)
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                await action(this);
            });
    }

    /// <summary>
    /// Refreshes the display (used within live display context).
    /// </summary>
    public void Refresh()
    {
        lock (_lock)
        {
            UpdateHeader();
            UpdateContent();
        }
    }

    private Panel CreateHeaderPanel()
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

        return new Panel(table)
        {
            Header = new PanelHeader("[bold blue]Conversation Information[/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue)
        };
    }

    private void UpdateHeader()
    {
        var updatedHeader = CreateHeaderPanel();
        _layout["Header"].Update(updatedHeader);
    }

    private void UpdateContent()
    {
        var content = string.Join(Environment.NewLine, _contentLines);
        var updatedContent = new Panel(new Markup(content))
        {
            Border = BoxBorder.None,
            Padding = new Padding(0, 0, 0, 0)
        };
        _layout["Content"].Update(updatedContent);
    }

    /// <summary>
    /// Disposes the resources used by the sticky header console.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
