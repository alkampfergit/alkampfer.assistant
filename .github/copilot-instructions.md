# AI agent instructions for this repo

This solution targets .NET 9 and is organized into three projects:
- Core (`src/Alkampfer.Assistant.Core`): domain primitives, identity/counter system, repositories, Semantic Kernel configurator.
- Host (`src/Alkampfer.Assistant.Host`): Blazor Server app using MudBlazor; loads optional override config.
- Tests (`src/Alkampfer.Assistant.Tests`): xUnit tests driving behavior and conventions.

## Important rules

### Elasticsearch related files.

For elasticsearch we will have files with double extensions, xxx.es7.cs and xxx.es8.cs to distinguish between concrete implementations that connect to different version of elasticsearch.

The rules for these files are are contained in

[Elasticsearch8](../rules/elastic8.md).
[Elasticsearch7](../rules/elastic7.md). 

## Architecture and flows
- Identity system
  - Base type: `Core/Identity.cs` with read-only `Value` ("prefix/numeric") and `NumericId`; derived types implement `protected override string Prefix` and MUST expose constructors `(string value)` and `(long numericId)`.
  - Manager: `Core/IDentityManager.cs` (`IdentityManager`) maps `prefix <-> type` via `RegisterIdentityType<T>()` (validated using Fasterflect). Usage:
    - Parse: `_manager.Parse("user/123")` returns correct derived type or throws for unknown or invalid format.
    - Generate: `_manager.GenerateNewAsync<TIdentity>()` or `_manager.GenerateNewAsync(prefix)`; both use an `ICounterManager` backend.
  - Counters: `ICounterManager` has `InitSeedAsync` and `GenerateNewCounterAsync`.
    - Persistence backends: `LiteDbCounterManager` and `MongoCounterManager` REQUIRE `InitSeedAsync(prefix, seed)` before first `Generate…` or they throw.
    - Tests use `InMemoryCounterManager` (no explicit init; first value is 1).
- Persistence
  - Contract: `IRepository<T>` (async CRUD + `AsQueryable`). Entities inherit `BaseEntity` with non-empty `Id`.
  - Implementations:
    - `MongoRepository<T>` uses `IMongoCollection<T>`; `AsQueryable` is server-backed (Mongo LINQ).
    - `LiteDbRepository<T>` opens a DB per call; `AsQueryable` materializes with `FindAll().ToList().AsQueryable()` (in-memory; OK for small data only).
- Semantic Kernel
  - `SemanticKernel/SemanticKernelConfigurator.cs` wires `AddOpenAIChatCompletion` with custom endpoint, model ID, API key, Serilog logging, and extended HttpClient timeout. Callers pass `endpointUrl`, `modelName`, `apiKey`.
- Host (Blazor Server)
  - `Program.cs` sets up MudBlazor services and Razor Components. `ConfigurationHelper.AddOverrideConfiguration` loads `alkampfer.assistant.json` from the current or parent directories (first-match), enabling local secrets/overrides without changing repo files.

## Conventions and behaviors (backed by tests)
- Throw `ArgumentException` for invalid input (null/empty/format errors), `InvalidOperationException` for misconfiguration (unknown prefix, missing constructor, uninitialized counter).
- Identity types must be registered before parse/generate; registering the same type twice is allowed and overwrites.
- Numeric IDs are non-negative `long`; parsing preserves `Value` formatting (leading zeros) while `NumericId` is the parsed number.

## Developer workflows (pwsh)
- Full CI-like flow (tools restore, GitVersion, tests with coverage, release build):
  ```pwsh
  ./build.ps1
  ```
- Quick tests with coverage artifact (drops into `TestResults/`):
  ```pwsh
  dotnet test src/Alkampfer.Assistant.Tests/Alkampfer.Assistant.Tests.csproj --collect:"XPlat Code Coverage" --results-directory TestResults -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
  ```
- Run the web host locally:
  ```pwsh
  dotnet run --project src/Alkampfer.Assistant.Host/Alkampfer.Assistant.Host.csproj
  ```

## Practical patterns and examples
- Adding a new identity type
  - Create `public sealed class OrderId : Identity` with both constructors and `Prefix => "order"`.
  - At startup, call `_manager.RegisterIdentityType<OrderId>();` and, for persistent counters, `counter.InitSeedAsync("order", seed)` once.
- Adding a new storage backend
  - Implement `IRepository<T>` and (usually) a matching `ICounterManager`. Prefer server-backed `AsQueryable`; avoid materializing to lists unless necessary.
- Using repositories
  - Ensure `entity.Id` is set (often to an `Identity.Value`) before `SaveAsync`; `MongoRepository` upserts by `Id`, `LiteDbRepository` `Upsert`s.

## External dependencies
- Central Package Management: `src/Directory.Packages.props` pins versions (Fasterflect, LiteDB, MongoDB.Driver, SemanticKernel, MudBlazor, xUnit, coverlet, Serilog).
- Versioning: `build.ps1` uses `dotnet-gitversion` configured by `.config/GitVersion.yml` (not committed here) to produce assembly/nuget versions.

Questions or gaps? Tell me which section needs more detail (e.g., DI wiring for SK, expected config keys, or storage initialization), and I’ll refine this file.
