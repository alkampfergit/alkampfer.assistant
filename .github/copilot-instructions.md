# Copilot Instructions for Alkampfer.Assistant

## Architecture Overview
This is a modular .NET 9.0 application with clean architecture:
- **Alkampfer.Assistant.Core**: Core business logic with interfaces and implementations
- **Alkampfer.Assistant.Host**: Blazor Server UI using MudBlazor
- **Alkampfer.Assistant.Tests**: xUnit tests

Supports pluggable data storage (LiteDB/MongoDB) and AI integration via Semantic Kernel.

## Key Components
- **Managers**: ICounterManager, IIdentityManager for business operations
- **Repositories**: IRepository<T> for data access abstraction
- **Entities**: BaseEntity, Identity with prefix/numeric format (e.g., "user/123")
- **Semantic Kernel**: Configured for OpenAI chat completion in SemanticKernelConfigurator.cs

## Development Workflow
- Build: `dotnet build` (uses Directory.Build.props for common settings)
- Test: `dotnet test` (xUnit with coverlet)
- Run: `dotnet run --project src/Alkampfer.Assistant.Host` (Blazor Server on port 5000/5001)
- Packages: Managed centrally in Directory.Packages.props

## Conventions and Patterns
- **Central Package Management**: All versions in Directory.Packages.props
- **Repository Pattern**: Generic IRepository<T> with LiteDB/MongoDB implementations
- **Manager Pattern**: Business logic in managers (e.g., counter generation)
- **Identity Format**: "prefix/numericId" (e.g., "counter/1") parsed in Identity class
- **Async by Default**: All operations are async with CancellationToken
- **Nullable Enabled**: Strict null checking

## Examples
- Add new counter: Implement ICounterManager in new integration folder
- Create entity: Inherit from BaseEntity, use IRepository<T>
- Register identity: Call IIdentityManager.RegisterIdentityType<T>() in DI setup
- UI component: Use MudBlazor in .razor files with @rendermode InteractiveServer

Focus on interfaces in Core, implementations in subfolders. Always test with xUnit.