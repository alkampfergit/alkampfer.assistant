# Blazor rules

The software uses an ui written in blazor with server side rendering and using [Mudblazor Components](https://www.mudblazor.com/docs/overview).

## General rules

- Split large components into smaller, reusable child components.
- Use always a separate class file with .razor.cs extension for page logic.
- Do not include code in the .razor file 
- Avoid directly mutating bound parameters ([Parameter]) in child components.
- Use EventCallback<T> instead of Action or custom delegates for parameter events.
- Use CascadingParameter for passing data like authentication state, theme, or culture.
- Prefer OnInitializedAsync() over OnInitialized() when using await.

## Component Architecture
- Organize components by domain/feature in folders (e.g., `Pages/`, `Components/`, `Shared/`).
- Follow the MVU or MVVM pattern when the state becomes complex.
- Use `@inject` for dependency injection rather than service locators.
- Prefer `RenderFragment` over `MarkupString` unless you need raw HTML rendering.

##  Performance Best Practices
- Minimize re-rendering by using `ShouldRender()` or conditional UI logic.
- Use `@key` in `@foreach` loops to help Blazor track DOM elements.
- Avoid using `async void`; use `async Task` instead.
- Dispose components that use resources by implementing `IDisposable`.

## Reusability and Maintainability
- Prefer `RenderFragment` parameters to allow child content injection (similar to slot in other frameworks).
- Isolate reusable logic in services or base classes.
- Use feature-based folders to group pages, components, and services.

## Testing & Tooling
- Use `bUnit` for unit testing Blazor components.
- Mock services using `NSubstitute` in test projects.
- Use `IJSRuntime` abstraction for JavaScript interop, and mock it in tests.
- Validate components for accessibility (ARIA, keyboard navigation).

## Debugging and Diagnostics
- Use `@ref` cautiously to avoid tight coupling.
- Enable detailed error messages in development mode.
- Use browser dev tools and Blazor’s built-in error boundaries.