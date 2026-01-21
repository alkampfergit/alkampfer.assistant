---
name: blazor-development
description: Build and modify Blazor Server-side components using MudBlazor. Use when creating Blazor pages, components, or UI elements, when the user mentions Blazor, Razor files, MudBlazor components, or when working with .razor or .razor.cs files.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(dotnet:*)
user-invocable: true
---

# Blazor Development Skill

## Core Principles

When working with Blazor components:

1. **MUST** separate logic into `.razor.cs` code-behind files - never inline code in `.razor` files
2. **MUST** use MudBlazor components from [mudblazor.com](https://www.mudblazor.com/docs/overview)
3. **MUST** split large components into smaller, reusable child components
4. **MUST** use `EventCallback<T>` for parameter events, not `Action` or custom delegates
5. **MUST** use `async Task` methods, never `async void`

## Quick Reference

For complete architectural rules and patterns, see: [memories/ui/blazor.md](../../memories/ui/blazor.md)

## Common Patterns

### Component Structure

```
Pages/
  MyPage.razor          # UI markup only
  MyPage.razor.cs       # All logic here
Components/
  MyComponent.razor
  MyComponent.razor.cs
Shared/
  MainLayout.razor
```

### Code-Behind Template

```csharp
public partial class MyComponent : ComponentBase
{
    [Inject] private IMyService MyService { get; set; } = default!;

    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> OnValueChanged { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Initialization logic
        await base.OnInitializedAsync();
    }

    private async Task HandleClick()
    {
        await OnValueChanged.InvokeAsync("new-value");
    }
}
```

### Performance Optimization

- Use `@key` in `@foreach` loops to help Blazor track DOM elements
- Implement `IDisposable` for components with resources
- Consider overriding `ShouldRender()` to minimize re-rendering

### Dependency Injection

- Use `@inject` directive in `.razor` files OR
- Use `[Inject]` property attribute in `.razor.cs` files
- Never use service locators

## Testing

- Unit test components using `bUnit`
- Mock services using `NSubstitute`
- Mock `IJSRuntime` for JavaScript interop testing
- Validate accessibility (ARIA, keyboard navigation)

## Workflow

When asked to create or modify Blazor components:

1. **Read** existing component structure to understand patterns
2. **Create/Edit** `.razor` file with markup only
3. **Create/Edit** `.razor.cs` file with all logic
4. **Verify** MudBlazor component usage is correct
5. **Ensure** proper parameter binding and event callbacks
6. **Check** for proper disposal if resources are used

## MudBlazor Component Reference

Common MudBlazor components:
- `MudButton`, `MudIconButton`
- `MudTextField`, `MudSelect`, `MudCheckBox`
- `MudTable`, `MudDataGrid`
- `MudDialog`, `MudDrawer`
- `MudCard`, `MudPaper`
- `MudAppBar`, `MudNavMenu`

For detailed component APIs, refer to [MudBlazor documentation](https://www.mudblazor.com/components).

## Important Rules from Reference

Before making any changes, **always** consult [memories/ui/blazor.md](../../memories/ui/blazor.md) for:
- Component architecture patterns (MVU/MVVM)
- Performance best practices
- Reusability guidelines
- Testing requirements
- Debugging approaches

## Anti-Patterns to Avoid

- ❌ Code in `.razor` files (except minimal rendering logic)
- ❌ Mutating `[Parameter]` properties directly
- ❌ Using `async void` methods
- ❌ Missing `@key` in loops with dynamic collections
- ❌ Forgetting to dispose resources
- ❌ Using `Action` instead of `EventCallback<T>`
