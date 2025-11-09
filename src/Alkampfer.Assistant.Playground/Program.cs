using System.Reflection;
using Alkampfer.Assistant.Core;
using Spectre.Console;

namespace Alkampfer.Assistant.Playground;

class Program
{
    static async Task Main(string[] args)
    {
        // Initialize environment variables from .env file
        DotEnv.Load();

        AnsiConsole.Write(
            new FigletText("Assistant Playground")
                .Centered()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[green]Welcome to Alkampfer.Assistant Playground![/]");
        AnsiConsole.MarkupLine("[dim]This is a console application for testing and experimenting with the assistant components.[/]");
        AnsiConsole.WriteLine();

        // Discover all examples
        var examples = DiscoverExamples();

        if (examples.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No examples found![/]");
            return;
        }

        // Main loop
        while (true)
        {
            var selectedExample = await ShowMainMenuAsync(examples);
            
            if (selectedExample == null)
            {
                break; // User chose to exit
            }

            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[yellow]{selectedExample.Name}[/]").LeftJustified());
            
            if (!string.IsNullOrEmpty(selectedExample.Description))
            {
                AnsiConsole.MarkupLine($"[dim]{selectedExample.Description}[/]");
            }
            
            AnsiConsole.WriteLine();

            try
            {
                await selectedExample.ExecuteAsync();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
        }

        AnsiConsole.MarkupLine("[green]Goodbye![/]");
    }

    private static List<ExampleBase> DiscoverExamples()
    {
        var exampleType = typeof(ExampleBase);
        var assembly = Assembly.GetExecutingAssembly();

        var examples = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(exampleType))
            .Select(t => (ExampleBase)Activator.CreateInstance(t)!)
            .OrderBy(e => e.Category ?? string.Empty)
            .ThenBy(e => e.Name)
            .ToList();

        return examples;
    }

    private static async Task<ExampleBase?> ShowMainMenuAsync(List<ExampleBase> examples)
    {
        // Group examples by category
        var categorized = examples
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? null : e.Category)
            .OrderBy(g => g.Key ?? string.Empty)
            .ToList();

        // Build menu choices
        var choices = new List<string>();
        var exampleMap = new Dictionary<string, ExampleBase>();

        // Add uncategorized examples first
        var uncategorized = categorized.FirstOrDefault(g => g.Key == null);
        if (uncategorized != null)
        {
            foreach (var example in uncategorized)
            {
                var choice = $"→ {example.Name}";
                choices.Add(choice);
                exampleMap[choice] = example;
            }
        }

        // Add categories
        var categories = categorized.Where(g => g.Key != null).ToList();
        if (categories.Any())
        {
            if (uncategorized != null && uncategorized.Any())
            {
                choices.Add("─────────────────");
            }

            foreach (var category in categories)
            {
                choices.Add($"📁 {category.Key}");
            }
        }

        choices.Add("─────────────────");
        choices.Add("❌ Exit");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Select an example to run:[/]")
                .PageSize(15)
                .MoreChoicesText("[dim](Move up and down to reveal more options)[/]")
                .AddChoices(choices));

        if (selection == "❌ Exit")
        {
            return null;
        }

        if (selection == "─────────────────")
        {
            return await ShowMainMenuAsync(examples);
        }

        // Check if it's a direct example
        if (exampleMap.TryGetValue(selection, out var directExample))
        {
            return directExample;
        }

        // It's a category, show category menu
        var categoryName = selection.Replace("📁 ", string.Empty);
        return await ShowCategoryMenuAsync(examples, categoryName);
    }

    private static async Task<ExampleBase?> ShowCategoryMenuAsync(List<ExampleBase> allExamples, string categoryName)
    {
        var categoryExamples = allExamples
            .Where(e => e.Category == categoryName)
            .OrderBy(e => e.Name)
            .ToList();

        var choices = categoryExamples.Select(e => $"→ {e.Name}").ToList();
        choices.Add("─────────────────");
        choices.Add("⬅️  Back to Main Menu");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[yellow]{categoryName}:[/]")
                .PageSize(15)
                .MoreChoicesText("[dim](Move up and down to reveal more options)[/]")
                .AddChoices(choices));

        if (selection == "⬅️  Back to Main Menu" || selection == "─────────────────")
        {
            return await ShowMainMenuAsync(allExamples);
        }

        var exampleName = selection.Replace("→ ", string.Empty);
        return categoryExamples.First(e => e.Name == exampleName);
    }
}
