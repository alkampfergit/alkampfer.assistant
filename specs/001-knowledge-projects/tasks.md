# Tasks: Bookmark Manager

**Input**: Design documents from `/specs/001-knowledge-projects/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Tests are included per constitution requirement (Test-First and CI-Gated Changes).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- Interfaces: `src/Alkampfer.Assistant.Interfaces/`
- Core implementations: `src/Alkampfer.Assistant.Core/`
- Host/UI: `src/Alkampfer.Assistant.Host/`
- Tests: `src/Alkampfer.Assistant.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, NuGet packages, and basic structure

- [x] T001 Add NuGet packages to `src/Directory.Packages.props`: SmartReader, ReverseMarkdown, Microsoft.Playwright, Azure.Storage.Blobs
- [x] T002 [P] Create `MemoryId` identity class in `src/Alkampfer.Assistant.Interfaces/Memories/MemoryId.cs`
- [x] T003 [P] Create `BookmarkId` identity class in `src/Alkampfer.Assistant.Interfaces/Bookmarks/BookmarkId.cs`
- [x] T004 [P] Create `IFileStore` interface in `src/Alkampfer.Assistant.Interfaces/IFileStore.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Entity Models

- [x] T005 [P] Create `Memory` entity in `src/Alkampfer.Assistant.Interfaces/Memories/Memory.cs`
- [x] T006 [P] Create `Bookmark` entity in `src/Alkampfer.Assistant.Interfaces/Bookmarks/Bookmark.cs`
- [x] T007 [P] Create `BookmarkStatus` enum in `src/Alkampfer.Assistant.Interfaces/Bookmarks/BookmarkStatus.cs`
- [x] T008 [P] Create `Attachment` record in `src/Alkampfer.Assistant.Interfaces/Memories/Attachment.cs`

### File Store Implementations

- [x] T009 Create `LocalFileStore` implementation in `src/Alkampfer.Assistant.Core/FileStore/LocalFileStore.cs`
- [x] T010 Create `AzureBlobFileStore` implementation in `src/Alkampfer.Assistant.Core/FileStore/AzureBlobFileStore.cs`
- [x] T011 Create `FileStoreConfiguration` and DI registration in `src/Alkampfer.Assistant.Core/FileStore/FileStoreExtensions.cs`

### Unit Tests for Foundational Components

- [x] T012 [P] Create `LocalFileStoreTests` in `src/Alkampfer.Assistant.Tests/Core/FileStore/LocalFileStoreTests.cs`
- [x] T012b [P] Create `AzureBlobFileStoreTests` in `src/Alkampfer.Assistant.Tests/Core/FileStore/AzureBlobFileStoreTests.cs`
- [x] T013 [P] Create `MemoryTests` in `src/Alkampfer.Assistant.Tests/Core/Memories/MemoryTests.cs`
- [x] T014 [P] Create `BookmarkTests` in `src/Alkampfer.Assistant.Tests/Core/Bookmarks/BookmarkTests.cs`

**Checkpoint**: Foundation ready — entities, file store abstraction, and basic tests in place

---

## Phase 3: User Story 1 — Bookmark Cataloging and Memory (Priority: P1) 🎯 MVP

**Goal**: Users can save URLs as bookmarks with tags, download content as Markdown, and store as Memory

**Independent Test**: Add a URL, trigger download, verify content is stored and status is "Downloaded"

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T015 [P] [US1] Contract test for Bookmark CRUD in `src/Alkampfer.Assistant.Tests/Core/Bookmarks/BookmarkRepositoryTests.cs`
- [x] T016 [P] [US1] Contract test for Memory CRUD in `src/Alkampfer.Assistant.Tests/Core/Memories/MemoryRepositoryTests.cs`
- [x] T017 [P] [US1] Unit test for `ContentExtractionService` in `src/Alkampfer.Assistant.Tests/Core/Bookmarks/ContentExtractionServiceTests.cs`
- [x] T018 [US1] Integration test for bookmark download workflow in `src/Alkampfer.Assistant.Tests/Integration/BookmarkWorkflowTests.cs`

### Implementation for User Story 1

#### Content Extraction Service

- [x] T019 [US1] Create `IContentExtractionService` interface in `src/Alkampfer.Assistant.Interfaces/Bookmarks/IContentExtractionService.cs`
- [x] T020 [US1] Implement `ContentExtractionService` with SmartReader + ReverseMarkdown in `src/Alkampfer.Assistant.Core/Bookmarks/ContentExtractionService.cs`
- [x] T021 [US1] Implement Playwright fallback in `ContentExtractionService` for JS-heavy pages
- [x] T022 [US1] Implement image downloading and Markdown link rewriting in `ContentExtractionService`

#### Bookmark Service

- [x] T023 [US1] Create `IBookmarkService` interface in `src/Alkampfer.Assistant.Interfaces/Bookmarks/IBookmarkService.cs`
- [x] T024 [US1] Implement `BookmarkService` in `src/Alkampfer.Assistant.Core/Bookmarks/BookmarkService.cs`
- [x] T025 [US1] Implement `CreateBookmarkAsync` method (FR-001)
- [x] T026 [US1] Implement `GetBookmarkAsync`, `ListBookmarksAsync`, `UpdateBookmarkAsync`, `DeleteBookmarkAsync` methods (FR-001)
- [x] T027 [US1] Implement `DownloadContentAsync` method — orchestrates extraction → Memory creation → Bookmark update (FR-002, FR-003)
- [x] T028 [US1] Add error handling, retry support, and status updates for download failures (Edge Cases)

#### Manual Content Upload

- [x] T029 [US1] Implement `UploadContentAsync` method for manual Markdown paste/upload (FR-004)
- [ ] T030 [US1] Implement zip file handling (markdown + images) in `BookmarkService` (FR-004)

#### Memory Service

- [x] T031 [US1] Create `IMemoryService` interface in `src/Alkampfer.Assistant.Interfaces/Memories/IMemoryService.cs`
- [x] T032 [US1] Implement `MemoryService` in `src/Alkampfer.Assistant.Core/Memories/MemoryService.cs`
- [x] T033 [US1] Implement `CreateMemoryAsync`, `GetMemoryAsync`, `GetContentAsync`, `DeleteMemoryAsync` methods

#### DI Registration

- [x] T034 [US1] Register Bookmark and Memory services in `src/Alkampfer.Assistant.Core/ServiceCollectionExtensions.cs`

**Checkpoint**: Core bookmark cataloging and memory extraction complete — can be tested via unit/integration tests

---

## Phase 4: User Story 1 — Blazor UI (Priority: P1 continued)

**Goal**: Blazor UI pages for bookmark management

**Independent Test**: Navigate to bookmarks page, add a URL, trigger download, view content

### Blazor Components

- [x] T035 [P] [US1] Create `BookmarkList.razor` component in `src/Alkampfer.Assistant.Host/Components/Pages/Bookmarks/BookmarkList.razor`
- [x] T036 [P] [US1] Create `BookmarkDetail.razor` component in `src/Alkampfer.Assistant.Host/Components/Pages/Bookmarks/BookmarkDetail.razor`
- [x] T037 [P] [US1] Create `AddBookmark.razor` component in `src/Alkampfer.Assistant.Host/Components/Pages/Bookmarks/AddBookmark.razor`
- [x] T038 [US1] Create `MemoryViewer.razor` component to display Markdown content in `src/Alkampfer.Assistant.Host/Components/Pages/Bookmarks/MemoryViewer.razor`
- [x] T039 [US1] Create `ManualContentUpload.razor` component for paste/upload in `src/Alkampfer.Assistant.Host/Components/Pages/Bookmarks/ManualContentUpload.razor`

### Navigation and Routing

- [x] T040 [US1] Add bookmarks navigation link to `NavMenu.razor` in `src/Alkampfer.Assistant.Host/Components/Layout/NavMenu.razor`
- [x] T041 [US1] Register services in `Program.cs` — call `AddBookmarkServices()` and `AddFileStore()` in `src/Alkampfer.Assistant.Host/Program.cs`

### Configuration

- [x] T042 [US1] Add `FileStore` configuration section to `appsettings.json` and `appsettings.Development.json`

**Checkpoint**: User Story 1 complete — users can manage bookmarks via Blazor UI

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect the entire feature

- [x] T043 [P] Add structured logging to `ContentExtractionService` and `BookmarkService`
- [x] T044 [P] Add XML documentation to public interfaces (`IFileStore`, `IBookmarkService`, `IMemoryService`)
- [ ] T045 Run `quickstart.md` validation — verify dev setup instructions work
- [x] T046 [P] Add README section for Bookmark Manager feature in `src/Alkampfer.Assistant.Host/README.md`
- [ ] T047 Performance validation — verify download completes in <30 seconds (SC-001)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 completion — BLOCKS all user story work
- **Phase 3 (US1 Core)**: Depends on Phase 2 completion
- **Phase 4 (US1 UI)**: Depends on Phase 3 completion (services must exist for UI)
- **Phase 5 (Polish)**: Depends on Phase 4 completion

### Within Each Phase

- Tests marked [P] can run in parallel
- Models/interfaces before implementations
- Services before UI components
- Core implementation before integration

### Parallel Opportunities by Phase

**Phase 1 (Setup)**:
```
T002, T003, T004 — all [P], can run in parallel
```

**Phase 2 (Foundational)**:
```
T005, T006, T007, T008 — all [P], can run in parallel (entities)
T009, T010 — T010 is [P] but T009 should complete first for pattern reference
T012, T013, T014 — all [P], can run in parallel (tests)
```

**Phase 3 (US1 Core)**:
```
T015, T016, T017 — all [P], can run in parallel (tests first!)
```

**Phase 4 (US1 UI)**:
```
T035, T036, T037 — all [P], can run in parallel (UI components)
```

---

## Implementation Strategy

### MVP Delivery (User Story 1)

1. Complete Phase 1: Setup (T001–T004)
2. Complete Phase 2: Foundational (T005–T014)
3. Complete Phase 3: US1 Core (T015–T034)
4. Complete Phase 4: US1 UI (T035–T042)
5. **VALIDATE**: Run all tests, verify workflow end-to-end
6. Complete Phase 5: Polish (T043–T047)

### Task Count Summary

| Phase | Task Count | Parallel Tasks |
|-------|------------|----------------|
| Phase 1: Setup | 4 | 3 |
| Phase 2: Foundational | 10 | 8 |
| Phase 3: US1 Core | 20 | 4 |
| Phase 4: US1 UI | 8 | 3 |
| Phase 5: Polish | 5 | 3 |
| **Total** | **47** | **21** |

---

## Notes

- All tasks follow constitution principle 3 (Test-First): tests T015–T018 must FAIL before implementation
- [P] tasks = different files, no dependencies — can parallelize
- [US1] label maps task to User Story 1 for traceability
- Commit after each task or logical group
- Stop at any checkpoint to validate independently
