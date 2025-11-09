using Spectre.Console;

namespace Alkampfer.Assistant.Playground.Examples.Advanced;

/// <summary>
/// Example demonstrating table rendering.
/// </summary>
public class TableExample : ExampleBase
{
    public override string Name => "Table Rendering";

    public override string Category => "Advanced Examples";

    public override string Description => "Demonstrates creating and rendering tables";

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[yellow]Table Rendering Example[/]");
        
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn(new TableColumn("Age").Centered());
        table.AddColumn("City");
        
        table.AddRow("Alice", "25", "New York");
        table.AddRow("Bob", "32", "London");
        table.AddRow("Charlie", "28", "Tokyo");
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
    }
}
