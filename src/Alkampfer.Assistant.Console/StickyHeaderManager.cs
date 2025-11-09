using Spectre.Console;
using Spectre.Console.Rendering;

namespace Alkampfer.Assistant.Console;

/// <summary>
/// Manages a sticky header that stays at the top of the console while content scrolls below.
/// </summary>
public class StickyHeaderManager : IDisposable
{
    private readonly object _lock = new();
    private readonly List<string> _contentLines = new();
    private int _headerHeight;
    private int _contentStartLine;
    private int _maxContentLines;
    private int _scrollOffset;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StickyHeaderManager"/> class.
    /// </summary>
    public StickyHeaderManager()
    {
        // Reserve space for header
        _headerHeight = 6; // Approximate height of the header panel
        _contentStartLine = _headerHeight + 1;
        CalculateContentArea();
    }

    /// <summary>
    /// Gets the number of lines available for content.
    /// </summary>
    public int MaxContentLines => _maxContentLines;

    /// <summary>
    /// Gets the current scroll offset.
    /// </summary>
    public int ScrollOffset => _scrollOffset;

    /// <summary>
    /// Gets the total number of content lines.
    /// </summary>
    public int TotalContentLines
    {
        get
        {
            lock (_lock)
            {
                return _contentLines.Count;
            }
        }
    }

    /// <summary>
    /// Renders the header at the top of the console.
    /// </summary>
    /// <param name="headerRenderable">The header content to render.</param>
    public void RenderHeader(IRenderable headerRenderable)
    {
        lock (_lock)
        {
            // Save cursor position
            var originalLeft = System.Console.CursorLeft;
            var originalTop = System.Console.CursorTop;

            // Move to top and render header
            System.Console.SetCursorPosition(0, 0);
            AnsiConsole.Write(headerRenderable);

            // Draw a separator line
            System.Console.SetCursorPosition(0, _headerHeight);
            AnsiConsole.Write(new Rule().RuleStyle("dim"));

            // Restore cursor position (or move to content area)
            try
            {
                System.Console.SetCursorPosition(originalLeft, Math.Max(originalTop, _contentStartLine));
            }
            catch
            {
                // If restoration fails, just move to content start
                System.Console.SetCursorPosition(0, _contentStartLine);
            }
        }
    }

    /// <summary>
    /// Adds a line to the content area.
    /// </summary>
    /// <param name="line">The line to add.</param>
    public void AddContentLine(string line)
    {
        lock (_lock)
        {
            _contentLines.Add(line);

            // Auto-scroll to bottom if we're already at the bottom
            if (_scrollOffset == GetMaxScrollOffset())
            {
                _scrollOffset = GetMaxScrollOffset();
            }
        }
    }

    /// <summary>
    /// Clears all content lines.
    /// </summary>
    public void ClearContent()
    {
        lock (_lock)
        {
            _contentLines.Clear();
            _scrollOffset = 0;
        }
    }

    /// <summary>
    /// Renders the visible content area based on scroll offset.
    /// </summary>
    public void RenderContent()
    {
        lock (_lock)
        {
            var visibleLines = GetVisibleLines();

            // Clear content area
            for (int i = 0; i < _maxContentLines; i++)
            {
                System.Console.SetCursorPosition(0, _contentStartLine + i);
                System.Console.Write(new string(' ', System.Console.WindowWidth - 1));
            }

            // Render visible lines
            for (int i = 0; i < visibleLines.Count && i < _maxContentLines; i++)
            {
                System.Console.SetCursorPosition(0, _contentStartLine + i);
                AnsiConsole.Markup(visibleLines[i]);
            }

            // Show scroll indicator if there's more content
            if (_contentLines.Count > _maxContentLines)
            {
                System.Console.SetCursorPosition(System.Console.WindowWidth - 15, _contentStartLine);
                AnsiConsole.Markup($"[dim]({_scrollOffset + 1}-{Math.Min(_scrollOffset + _maxContentLines, _contentLines.Count)}/{_contentLines.Count})[/]");
            }
        }
    }

    /// <summary>
    /// Scrolls the content up by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll up.</param>
    /// <returns>True if scrolled, false if already at top.</returns>
    public bool ScrollUp(int lines = 1)
    {
        lock (_lock)
        {
            if (_scrollOffset > 0)
            {
                _scrollOffset = Math.Max(0, _scrollOffset - lines);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Scrolls the content down by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down.</param>
    /// <returns>True if scrolled, false if already at bottom.</returns>
    public bool ScrollDown(int lines = 1)
    {
        lock (_lock)
        {
            var maxOffset = GetMaxScrollOffset();
            if (_scrollOffset < maxOffset)
            {
                _scrollOffset = Math.Min(maxOffset, _scrollOffset + lines);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Scrolls to the bottom of the content.
    /// </summary>
    public void ScrollToBottom()
    {
        lock (_lock)
        {
            _scrollOffset = GetMaxScrollOffset();
        }
    }

    /// <summary>
    /// Scrolls to the top of the content.
    /// </summary>
    public void ScrollToTop()
    {
        lock (_lock)
        {
            _scrollOffset = 0;
        }
    }

    private List<string> GetVisibleLines()
    {
        var result = new List<string>();
        var startIndex = _scrollOffset;
        var endIndex = Math.Min(_scrollOffset + _maxContentLines, _contentLines.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            result.Add(_contentLines[i]);
        }

        return result;
    }

    private int GetMaxScrollOffset()
    {
        return Math.Max(0, _contentLines.Count - _maxContentLines);
    }

    private void CalculateContentArea()
    {
        // Calculate how many lines are available for content
        var windowHeight = System.Console.WindowHeight;
        _maxContentLines = Math.Max(5, windowHeight - _contentStartLine - 2); // Reserve 2 lines for input
    }

    /// <summary>
    /// Recalculates the content area when console is resized.
    /// </summary>
    public void OnResize()
    {
        lock (_lock)
        {
            CalculateContentArea();
        }
    }

    /// <summary>
    /// Disposes the resources.
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
