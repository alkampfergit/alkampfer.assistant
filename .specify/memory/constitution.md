<!--
Sync Impact Report

- Version change: 1.0.0 → 1.0.1
- Modified principles: none
- Added sections: none
- Removed sections: none
- Clarifications: IFileStore abstraction explicitly named in Constraints & Tech Stack
- Templates reviewed: .specify/templates/plan-template.md ✅, spec-template.md ✅,
	tasks-template.md ✅, checklist-template.md ✅, agent-file-template.md ✅
- Follow-up TODOs: none
-->

# Alkampfer.Assistant Constitution

## Core Principles

### 1. Modular Library-First Architecture
All new capabilities MUST be implemented as small, well-scoped libraries or components
within `src/`. Libraries MUST be independently buildable, documented, and covered
by unit tests; public interfaces (DTOs, `ILanguageModel`, repositories) are the
stable contract surface and MUST live in `Alkampfer.Assistant.Interfaces`.

### 2. Explicit LLM Interface Contract
All language-model adapters and related code MUST implement the interfaces in
`Alkampfer.Assistant.Interfaces.Llm`. Implementations MUST declare capabilities via
`GetCapability()` and MUST either support conversation-aware `LlmRequest` flows or
explicitly throw `NotSupportedException` when a capability is unavailable.

### 3. Test-First and CI-Gated Changes (NON-NEGOTIABLE)
Tests MUST be written before behavior changes are implemented for all non-trivial
features (unit + relevant integration tests). PRs MUST pass the repository CI and
include tests that reproduce bugs fixed. Critical behaviors around LLM request/response
flows and `ConversationAgent` MUST be covered by tests.

### 4. Integration, Contract, and Migration Tests
When changing public contracts (interfaces, DTOs, persisted schemas), authors MUST
add contract tests and a migration plan. Integration tests are REQUIRED for
interactions between `ConversationAgent`, LLM adapters, and persistence layers
(LiteDB/Mongo) where behavior cannot be validated with unit tests alone.

### 5. Observability, Simplicity, and Explicit Versioning
Libraries and services MUST emit structured logs and surface key metrics where
applicable. Keep designs simple (YAGNI). Follow semantic versioning for public
packages and APIs: MAJOR for breaking changes, MINOR for new backwards-compatible
features, PATCH for fixes and clarifications.

## Constraints & Tech Stack

- Runtime: .NET 9 (project targets in repository). All new code MUST target
	the repository's chosen SDK unless there is a justified exception.
- Project conventions: central package versions via `Directory.Packages.props`.
- Nullability: nullable reference types enabled; prefer explicit nullability.
- Async-first: public APIs SHOULD be async and accept `CancellationToken`.
- External adapters (Azure OpenAI etc.) live under `Alkampfer.Assistant.Core/Llm`.
- File and blob storage MUST use the `IFileStore` abstraction with pluggable backends
  (local filesystem default, Azure Blob opt-in). Implementations live in
  `Alkampfer.Assistant.Core/FileStore`.
- Identities are managed by a specific interface and are in the form of prefix/sequence 

## User interface

- Asp.NET Blazor server side hosted in the project `src/Alkampfer.Assistant.Host/Alkampfer.Assistant.Host.csproj`.
- The interface should be user friendly and intuitive.


## Development Workflow

- Branching: feature branches prefixed with `###-` as used by existing templates.
- Reviews: PRs MUST include rationale for complex changes, reference relevant
	tests, and include changelog entries when public contracts change.
- Tests: run `dotnet test` for the changed projects; critical tests (ConversationAgent)
	MUST be included in every PR that touches LLM/conversation logic.
- Releases: follow semantic versioning; bump MAJOR for breaking contract changes and
	provide migration notes in the PR.

## Governance

1. Amendments: Proposals to amend the constitution MUST be filed as a spec and
	 a PR that updates this file. Amendments require approval from at least one
	 repository maintainer and one core contributor (or the authoring team where
	 contributors are not defined). Changes that remove or redefine existing
	 principles are MAJOR-level and require documented migration guidance.

2. Versioning policy: The constitution itself follows semantic versioning. Minor
	 additions to guidance are a MINOR bump; wording clarifications or typo fixes
	 are a PATCH. MAJOR bumps occur when principles are removed or materially
	 redefined.

3. Compliance: Every release PR SHOULD reference the constitution sections it
	 affects. The CI pipeline SHOULD run an automated subset of checks (build + tests)
	 and human reviewers MUST verify principle compliance for complex changes.

**Version**: 1.0.1 | **Ratified**: 2025-12-06 | **Last Amended**: 2025-12-08
