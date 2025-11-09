using Spectre.Console;

namespace Alkampfer.Assistant.Playground.Examples;

/// <summary>
/// A simple example demonstrating basic console output.
/// This example has no category and will appear at the root level.
/// </summary>
public class HelloWorldExample : ExampleBase
{
    public override string Name => "Hello World";

    public override string Description => "A simple hello world example";

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[green]Hello World from Playground![/]");
        AnsiConsole.MarkupLine("[dim]This is a simple uncategorized example.[/]");
        
        await Task.CompletedTask;
    }
}
