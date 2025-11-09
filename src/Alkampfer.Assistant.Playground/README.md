# Alkampfer.Assistant.Playground

This is a playground project for testing and experimenting with the Alkampfer.Assistant components.

## Structure

The playground uses a menu-driven system powered by SpectreConsole to organize and run examples.

### Creating New Examples

1. Create a new class that inherits from `ExampleBase`
2. Implement the required properties and `ExecuteAsync` method:

```csharp
using Spectre.Console;

namespace Alkampfer.Assistant.Playground.Examples.YourCategory;

public class YourExample : ExampleBase
{
    public override string Name => "Your Example Name";
    
    // Optional: Set to null or omit for root-level examples
    public override string Category => "Your Category";
    
    public override string Description => "What your example does";
    
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[green]Your example code here[/]");
        await Task.CompletedTask;
    }
}
```

3. Examples are automatically discovered via reflection - no registration needed!

### Menu Navigation

- **Root Level**: Examples without a category appear at the top of the main menu
- **Categories**: Examples with the same category are grouped together in sub-menus
- **Navigation**: Use arrow keys to select, Enter to run, and follow the back buttons to return

### Running

```bash
dotnet run --project src/Alkampfer.Assistant.Playground
```

The application automatically loads environment variables from `.env` files using `DotEnv.Load()`.

### Environment Variables

Some examples require environment variables for configuration. Create a `.env` file in the Playground directory based on `.env.example`:

```bash
cp .env.example .env
```

For Azure OpenAI examples, you'll need:
- `AZURE_OPENAI_ENDPOINT`: Your Azure OpenAI endpoint URL
- `AZURE_OPENAI_API_KEY`: Your Azure OpenAI API key
- `AZURE_OPENAI_DEPLOYMENT_ID`: Your deployment/model name

## Purpose

This project provides a sandbox environment for:
- Testing Core and Interface implementations
- Prototyping new features
- Quick experiments with assistant components
- Learning and exploring the codebase

## Features

- Uses **Spectre.Console** for rich terminal output
- References both `Alkampfer.Assistant.Core` and `Alkampfer.Assistant.Interfaces`
- Console application with async support

## Running the Playground

```bash
dotnet run --project src/Alkampfer.Assistant.Playground
```

Or from the src directory:

```bash
dotnet run --project Alkampfer.Assistant.Playground
```

## Usage

Edit `Program.cs` to add your experimental code. The project already includes:
- Spectre.Console for beautiful console output
- Project references to Core and Interfaces
- Async/await support in Main method

## Examples

Add your test code in the `Main` method:

```csharp
// Example: Testing Identity functionality
var identity = new Identity("user/123");
AnsiConsole.MarkupLine($"[green]Identity:[/] {identity.Id}");
```
