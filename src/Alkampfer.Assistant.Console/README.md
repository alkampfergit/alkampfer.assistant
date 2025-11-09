# Alkampfer.Assistant.Console

Console UI components for the Alkampfer Assistant framework with support for sticky headers and scrollable content areas.

## Features

### Sticky Header Display
- **True sticky header** that remains at the top while content scrolls
- **Real-time statistics** showing conversation token usage
- **Scrollable content area** that can grow beyond screen height
- **Automatic scrolling** to bottom when new content is added

## Components

### ConversationDisplayInfo
Stores conversation statistics for display:
- Conversation active status
- Total input/output tokens
- Last call input/output tokens

### ScrollableConversationDisplay (Recommended)
The main component for conversation UIs with a truly sticky header.

**Features:**
- Header stays fixed at top of console
- Content area scrolls independently
- Automatic scroll-to-bottom on new messages
- Manual scrolling support (future enhancement)
- Token statistics updated in real-time

**Usage:**
```csharp
using var display = new ScrollableConversationDisplay();

// Initialize the display
display.UpdateFromConversation(conversation);
display.Initialize();

// Write content
display.WriteLine("[green]Message here[/]");
display.WriteLines("Line 1", "Line 2", "Line 3");

// Get user input
var input = display.PromptInput("[blue]You:[/]");

// Show status during async operations
var result = await display.ShowStatusAsync("Processing...",
    async () => await DoWorkAsync());

// Update header with new statistics
display.UpdateFromConversation(conversation);
display.RenderHeader();
```

### ConversationConsoleDisplay (Simple)
A simpler implementation that clears and re-renders the entire screen.

Good for:
- Simple conversations
- When screen clearing is acceptable
- Quick prototyping

### StickyHeaderManager (Low-level)
Low-level component for managing sticky headers with cursor positioning.

**Features:**
- Manual cursor positioning
- Content line management
- Scroll offset tracking
- Visible content rendering

## Architecture

The sticky header implementation uses console cursor positioning to:
1. Reserve the top portion of the screen for the header (6-7 lines)
2. Render content only in the content area below the header
3. Track which content lines are visible based on scroll offset
4. Re-render only the header when statistics change
5. Auto-scroll content area when new lines are added

This provides a true "sticky header" experience where:
- The header never scrolls off screen
- Content can grow beyond screen height
- Only the content area scrolls
- The header updates independently

## Example

See [ConversationAgentExample.cs](../Alkampfer.Assistant.Playground/Examples/ConversationAgentExample.cs) for a complete working example.

## Limitations

- Requires terminal that supports ANSI cursor positioning
- Console resizing is not fully handled (planned enhancement)
- Manual scrolling with arrow keys requires additional input handling (future enhancement)

## Future Enhancements

- [ ] Arrow key handling for manual scrolling
- [ ] Page up/down support
- [ ] Console resize handling
- [ ] Configurable header height
- [ ] Multiple sticky regions (header + footer)
