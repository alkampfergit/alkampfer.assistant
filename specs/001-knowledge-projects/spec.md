# Feature Specification: Bookmark Manager

**Feature Branch**: `001-knowledge-projects`
**Created**: 2025-12-06
**Status**: Draft
**Input**: User description: "I want to build an assistant that will use AI model to help me brainstorm and keep track of information. I need it to be able to keep track of url and bookmarks so I can catalogate my bookmark. Then it should be able to download the content of the bookmark if requested, storing it as memory. [Reduced Scope: Focus only on URL management and download]"

## User Scenarios & Testing

### User Story 1 - Bookmark Cataloging and Memory (Priority: P1)

As a user, I want to save URLs as bookmarks and have the assistant download their content so that I can reference them later.

**Why this priority**: This is the foundation of the knowledge base. Without data, the assistant cannot help brainstorm.

**Independent Test**: Can be tested by adding a URL, triggering download, and verifying the content is stored.

**Acceptance Scenarios**:

1. **Given** a valid URL, **When** I add it as a bookmark, **Then** it is saved in the catalog.
2. **Given** a saved bookmark, **When** I request to download its content, **Then** the system fetches the web page, extracts the main text, and stores it as a memory associated with the bookmark.
3. **Given** a bookmark with downloaded content, **When** I view the bookmark details, **Then** I can see the status is "Downloaded".

### Edge Cases

- **Invalid URL**: If a user adds an invalid or unreachable URL, the system should flag it as "Error" during download but keep the bookmark entry.
- **Download Failure**: If the download fails (e.g., 404, timeout), the system should allow retrying the download.

## Requirements

### Functional Requirements

- **FR-001**: System MUST allow users to create, read, update, and delete Bookmarks (URL, Title).
- **FR-002**: System MUST provide a mechanism to fetch and extract text content from a Bookmark's URL (Web Scraping).
- **FR-003**: System MUST store extracted content from Bookmarks as "Memory" available for retrieval.

- **FR-004**: System MUST allow users to manually insert or paste content for a Bookmark when automatic extraction is impossible or unsatisfactory.
- **FR-005**: When extracting content, the system SHOULD produce Markdown-formatted output and SHOULD download and persist inline images referenced by the page, storing image references in the `Memory` record (or filesystem-backed raw content) so the Markdown can reference local image paths.

**Storage note:** Extracted content MUST be stored as a separate `Memory` entity (not inlined on the `Bookmark`). Memory is a generic Markdown store with no metadata; all metadata lives on the referencing entity (Bookmark). Raw Markdown is persisted using a filesystem-backed abstraction. Bookmark references Memory via `memory_id`.

**Implementation note:** For FR-002 the implementation approach will be: Hybrid — attempt a basic HTTP fetch plus HTML main-text extraction first (Readability-like); on failure for JS-heavy or otherwise unparseable pages, fallback to headless rendering (e.g., Playwright/Puppeteer) to render and extract content.

Implementation detail: Extraction output format will be Markdown where possible. Images found during extraction will be downloaded and stored through the filesystem-backed abstraction; the extracted Markdown will update image `src` references to point to the local stored image paths. If extraction fails, the UI/API MUST offer an option for manual content insertion (paste or upload) which creates a `Memory` record with `content_format: markdown` (or `plaintext` if user chooses).

The user should be able to upload directly a text, markdown or a zip file containing a markdown file and corresponding images.

Implementation decision: Embeddings/indexing are deferred for this iteration. The system will store raw Markdown (and downloaded assets) without creating embeddings or a vector index; embedding generation and vector indexing are planned for a future iteration once core ingestion and retrieval are validated.

### Key Entities

- **Memory**: A generic, reusable entity for storing Markdown content. Memory has no metadata of its own; metadata is provided by the entity that references it. Contains ID (`MemoryId`), content_path, attachments, created_at, updated_at.
- **Bookmark**: A resource representing a URL. Contains ID (`BookmarkId`), Title, URL, Tags (array of string), Status (New, Downloaded, Error), memory_id (FK -> Memory, nullable).

## Key Entities

- **Memory**: A generic Markdown store. It knows nothing about the entity that uses it. Stored via a filesystem abstraction and referenced by `content_path`. Can be reused by other entity types in future iterations.
- **Bookmark**: A resource representing a URL. Owns all metadata (title, url, tags, status). Optionally references a Memory when content is downloaded or manually inserted.

### Memory fields

- id: `MemoryId` (subclass of `IDentity` class)
- content_path: string (filesystem path to raw Markdown file)
- attachments: array of { name, path }
- created_at: datetime
- updated_at: datetime

### Bookmark fields

- id: `BookmarkId` (subclass of `IDentity` class)
- title: string
- url: string
- tags: array of string
- status: enum { new, downloaded, error }
- last_error: string (nullable)
- memory_id: `MemoryId` (FK -> Memory.id, nullable)
- created_at: datetime
- updated_at: datetime

## Success Criteria

### Measurable Outcomes

- **SC-001**: Users can successfully download and store a standard web page (e.g., a blog post) in under 30 seconds.

## Assumptions & Out of Scope

- **Assumption**: "Memory" implies a mechanism for the LLM to access the text, likely via RAG (Retrieval Augmented Generation) or context window injection.
- **Out of Scope**: Document ingestion (uploading files) is excluded from this iteration.
- **Out of Scope**: Project definition and scoping are excluded from this iteration.
- **Out of Scope**: Context-aware brainstorming (asking questions to specific projects) is excluded from this iteration.
- **Out of Scope**: Web search (live) is excluded.

## Clarifications

### Session 2025-12-06

- Q: Which scraping approach should the implementation use for fetching bookmark content? → A: C (Hybrid: attempt basic fetch+extraction first, fallback to headless rendering on failure).
- Q: Which storage model should be used for extracted bookmark content? → A: B (Memory entity with filesystem-backed raw content).
- Q: Which embedding/indexing strategy should we use? → A: A (No embeddings/indexing now — store raw/Markdown only).
- Q: Should users be able to manually insert bookmark content when extraction fails? → A: Yes (allow paste/upload creating a `Memory` record with `extraction_status: manual`).
- Q: Should extracted content be converted to Markdown and images downloaded? → A: Yes (produce Markdown output; download images into filesystem abstraction and reference local paths).
- Q: Which storage model should be used for extracted bookmark content? → A: B (Memory entity with filesystem-backed raw content).
- Q: How should Bookmark and Memory entities be persisted? → A: B (Reuse existing `IRepository<T>` generic interface for both entities).
- Q: What methods should `IFileStore` expose for the filesystem abstraction? → A: C (Hybrid: both sync and async overloads).
- Q: Which `IFileStore` implementation should be the default? → A: B (Local filesystem is default; Azure Blob is opt-in via configuration).
- Q: Which library/approach for HTML main-text extraction + Markdown conversion? → A: Hybrid (Use `SmartReader` + `ReverseMarkdown` first; fallback to Playwright rendering if extraction fails or content is empty).
