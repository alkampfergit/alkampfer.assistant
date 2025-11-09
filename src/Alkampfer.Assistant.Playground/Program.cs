using Spectre.Console;

namespace Alkampfer.Assistant.Playground;

class Program
{
    static async Task Main(string[] args)
    {
        AnsiConsole.Write(
            new FigletText("Assistant Playground")
                .Centered()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[green]Welcome to Alkampfer.Assistant Playground![/]");
        AnsiConsole.MarkupLine("[dim]This is a console application for testing and experimenting with the assistant components.[/]");
        
        // Add your test code here
        await Task.CompletedTask;
    }
}
