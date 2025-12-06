# Copilot Instructions — Alkampfer.Assistant (concise)

Overview
- Modular .NET 9 application using a clean-architecture split:
  - `Alkampfer.Assistant.Interfaces` — public interfaces and shared DTOs (best place to start)
  - `Alkampfer.Assistant.Core` — business logic, managers, repositories, LLM adapters
  - `Alkampfer.Assistant.Host` — Blazor Server UI (MudBlazor)
  - `Alkampfer.Assistant.Tests` — xUnit tests covering core behaviors

Key patterns & files (practical shortcuts)
- LLM surface: `src/Alkampfer.Assistant.Interfaces/Llm` — `ILanguageModel.cs`, `LlmRequest.cs`, `LlmCapabilities.cs`, `LanguageModelResponse.cs`.
  - Two model styles supported: prompt-based `GenerateResponseAsync(string)` and request-based `GenerateResponseAsync(LlmRequest)`. Use `GetCapability()` to detect `SupportConversation`.
  - `LanguageModelResponse.ResponseId` is used to chain conversation-aware requests in `ConversationAgent`.
- Conversation handling: `src/Alkampfer.Assistant.Core/ConversationAgent.cs` — checks `ILanguageModel.GetCapability()`; if `SupportConversation` is true it sends a minimal `LlmRequest` with `PreviousConversationId`, otherwise builds a full prompt from `Conversation.GetMessagesAsync()`.
- Azure adapters: `src/Alkampfer.Assistant.Core/Llm/AzureOpenAiResponseLanguageModel.cs` (supports `PreviousResponseId`) and `AzureOpenAiChatLanguageModel.cs` (prompt-only). Follow their patterns when adding new providers.

Developer workflow (commands)
- Build solution: `dotnet build src/Alkampfer.Assistant.sln`
- Run tests: `dotnet test src/Alkampfer.Assistant.Tests/Alkampfer.Assistant.Tests.csproj`
- Run specific tests: `dotnet test --filter "FullyQualifiedName~ConversationAgentWithLlmRequestTests"`
- Run Host locally: `dotnet run --project src/Alkampfer.Assistant.Host`

Repository conventions
- Central package versions: `Directory.Packages.props`.
- Async + CancellationToken: prefer async methods and accept `CancellationToken`.
- Nullable enabled: prefer explicit nullability annotations.
- Identity format: `prefix/numeric` (see `Alkampfer.Assistant.Interfaces/Identity.cs`).
- Keep changes minimal and focused; preserve public interfaces in `Alkampfer.Assistant.Interfaces`.

Testing & mocks
- Tests use xUnit + Moq. See `src/Alkampfer.Assistant.Tests/Core/ConversationAgentTests.cs` and `ConversationAgentWithLlmRequestTests.cs`:
  - Use a helper mock pattern to set `GetCapability()` when mocking `ILanguageModel`.
  - For conversation-capable models assert `GenerateResponseAsync(LlmRequest)` is called and `LanguageModelResponse.ResponseId` flows to `ConversationAgent.LastResponseId`.

Implementation guidance for code edits
- Use small focused edits (apply_patch). Run unit tests after edits.
- If a model doesn't support a feature, explicitly throw `NotSupportedException` (see `AzureOpenAiChatLanguageModel`).
- Add DTOs or shared types to `Alkampfer.Assistant.Interfaces` rather than moving existing types.
- Avoid changing Directory.Build.* or Directory.Packages.props unless necessary.

Integration & external deps
- Azure OpenAI clients live in `Core/Llm`. Mind API versions used in `AzureOpenAIClientOptions` and `OpenAI.Responses` vs `OpenAI.Chat` usage.

Helpful entry points for new work
- Start with `src/Alkampfer.Assistant.Interfaces/Llm/ILanguageModel.cs` for LLM-related changes.
- Add managers/repositories by following `Core/LiteDbIntegration` and `Core/MongoDbIntegration` patterns.
- Use `src/Alkampfer.Assistant.Tests/Core` test files as canonical examples for unit test structure.

Notes for AI agents
- Do not add headers/licenses. Keep pull requests small and test-first.
- Use `manage_todo_list` to propose and track a short plan when making multi-step changes.
- After edits run `dotnet build` and `dotnet test` and include failing tests output if something breaks.

If you want, I can expand any section (DI registration, authoring an LLM adapter, or adding a manager) with concrete code snippets. Please tell me which area to expand.
