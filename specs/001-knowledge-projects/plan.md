# Implementation Plan: Bookmark Manager

**Branch**: `001-knowledge-projects` | **Date**: 2025-12-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-knowledge-projects/spec.md`

## Summary

Implement a Bookmark Manager that allows users to save URLs as bookmarks with tags, download/extract web page content as Markdown (with images), and store extracted content in a reusable Memory entity. Uses hybrid scraping (SmartReader first, Playwright fallback), filesystem-backed storage abstraction (`IFileStore` with local FS default, Azure Blob opt-in), and the existing `IRepository<T>` pattern for persistence.

## Technical Context

**Language/Version**: C# / .NET 9  
**Primary Dependencies**: SmartReader, ReverseMarkdown, Playwright (fallback), Azure.Storage.Blobs (opt-in)  
**Storage**: LiteDB/MongoDB via `IRepository<T>` for entities; local filesystem (default) or Azure Blob via `IFileStore` for raw Markdown/images  
**Testing**: xUnit (`dotnet test`)  
**Target Platform**: ASP.NET Blazor Server (Alkampfer.Assistant.Host)  
**Project Type**: Web application (Blazor server-side)  
**Performance Goals**: Download and index a standard web page in <30 seconds (SC-001)  
**Constraints**: Async-first APIs with CancellationToken; nullable reference types enabled  
**Scale/Scope**: Single-user assistant; ~1000s of bookmarks expected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| 1. Modular Library-First Architecture | ✅ PASS | New `IFileStore` abstraction and entity classes will live in `Alkampfer.Assistant.Interfaces`; implementations in `Alkampfer.Assistant.Core`. |
| 2. Explicit LLM Interface Contract | ✅ N/A | This feature does not add LLM adapters. |
| 3. Test-First and CI-Gated Changes | ✅ PASS | Unit tests for `IFileStore`, `Bookmark`, `Memory`, extraction service required before merge. |
| 4. Integration, Contract, and Migration Tests | ✅ PASS | Contract tests for new `IFileStore` and entity schemas; integration tests for extraction pipeline. |
| 5. Observability, Simplicity, and Explicit Versioning | ✅ PASS | Structured logging for extraction; YAGNI (no embeddings yet); semantic versioning for public APIs. |

**Post-Design Re-check**: Design adds `IFileStore` interface with two implementations (LocalFileStore, AzureBlobFileStore) and two new entities (Bookmark, Memory). All align with constitution principles.

## Project Structure

### Documentation (this feature)

```text
specs/001-knowledge-projects/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI)
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Alkampfer.Assistant.Interfaces/
│   ├── IFileStore.cs                 # New: filesystem abstraction interface
│   ├── Bookmarks/
│   │   ├── Bookmark.cs               # New: Bookmark entity
│   │   └── BookmarkId.cs             # New: IDentity subclass
│   └── Memories/
│       ├── Memory.cs                 # New: Memory entity
│       └── MemoryId.cs               # New: IDentity subclass
├── Alkampfer.Assistant.Core/
│   ├── FileStore/
│   │   ├── LocalFileStore.cs         # New: local filesystem implementation
│   │   └── AzureBlobFileStore.cs     # New: Azure Blob implementation
│   └── Bookmarks/
│       └── ContentExtractionService.cs  # New: scraping + markdown conversion
├── Alkampfer.Assistant.Host/
│   └── Components/
│       └── Pages/
│           └── Bookmarks/            # New: Blazor UI for bookmark management
└── Alkampfer.Assistant.Tests/
    ├── Core/
    │   ├── FileStore/
    │   │   └── LocalFileStoreTests.cs
    │   └── Bookmarks/
    │       └── ContentExtractionServiceTests.cs
    └── Integration/
        └── BookmarkWorkflowTests.cs
```

**Structure Decision**: Follows existing repository layout. New interfaces in `Alkampfer.Assistant.Interfaces`, implementations in `Alkampfer.Assistant.Core`, UI in `Alkampfer.Assistant.Host`, tests in `Alkampfer.Assistant.Tests`.

## Complexity Tracking

> No constitution violations requiring justification.
