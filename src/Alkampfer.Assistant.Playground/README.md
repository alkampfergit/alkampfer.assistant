# Alkampfer.Assistant.Playground

A console application for testing and experimenting with Alkampfer.Assistant components.

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
