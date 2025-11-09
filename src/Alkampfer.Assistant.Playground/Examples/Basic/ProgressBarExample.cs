using Spectre.Console;

namespace Alkampfer.Assistant.Playground.Examples.Basic;

/// <summary>
/// Example demonstrating progress bar functionality.
/// </summary>
public class ProgressBarExample : ExampleBase
{
    public override string Name => "Progress Bar Demo";

    public override string Category => "Basic Examples";

    public override string Description => "Shows a progress bar animation";

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[yellow]Progress Bar Example[/]");
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task1 = ctx.AddTask("[green]Processing data[/]");
                var task2 = ctx.AddTask("[blue]Analyzing results[/]");

                while (!ctx.IsFinished)
                {
                    await Task.Delay(100, cancellationToken);
                    task1.Increment(2);
                    task2.Increment(1.5);
                }
            });

        AnsiConsole.MarkupLine("[green]✓ Complete![/]");
    }
}
