using Spectre.Console;

namespace Alkampfer.Assistant.Playground.Examples.Basic;

/// <summary>
/// Example demonstrating basic console interactions.
/// </summary>
public class ConsoleInteractionExample : ExampleBase
{
    public override string Name => "Console Interaction";

    public override string Category => "Basic Examples";

    public override string Description => "Demonstrates basic console interaction";

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[yellow]Console Interaction Example[/]");
        
        var name = AnsiConsole.Ask<string>("What's your [green]name[/]?");
        AnsiConsole.MarkupLine($"[blue]Hello, {name}![/]");
        
        var age = AnsiConsole.Ask<int>("What's your [green]age[/]?");
        AnsiConsole.MarkupLine($"[blue]You are {age} years old.[/]");
        
        await Task.CompletedTask;
    }
}
