# Research: Bookmark Manager (Phase 0)

## Decisions

- Decision: Scraping approach — Hybrid
  - Rationale: Start with a fast HTTP fetch + main-text extraction (Readability-like) and only use headless rendering (Playwright) for pages that require JS execution. Balances speed and reliability.

- Decision: Storage model for extracted content — Separate `Memory` entity + filesystem-backed raw files
  - Rationale: Keeps `Bookmark` records lightweight, enables later indexing/embedding workflows, and allows storing large raw content and assets outside the DB.

- Decision: Raw storage backends — Local filesystem + Azure Blob Storage support (pluggable)
  - Rationale: Local filesystem for easy dev/test; Azure Blob for cloud durability and optional off-host storage. Implement filesystem abstraction to support both.

- Decision: Output format — Markdown with downloaded images
  - Rationale: Markdown is portable, readable, and easy to surface in UI; downloading images preserves context and allows offline rendering.

- Decision: Manual insertion — Supported
  - Rationale: Provide a fallback when automated extraction fails or user prefers to paste curated content.

- Decision: Embeddings/indexing — Deferred (no embeddings in initial iteration)
  - Rationale: Focus on ingestion, storage, and retrieval first; add embeddings once ingestion is stable.

## Alternatives Considered

- Full headless-first scraping: Rejected due to higher resource cost and latency.
- External parsing API (Diffbot): Rejected for now to avoid paid external dependency and to keep control of raw content.
- Inline text in Bookmark: Rejected to avoid bloating primary bookmark records and to simplify indexing later.

## Unknowns / NEEDS CLARIFICATION (resolved)

- File storage abstraction details: User requested local FS + Azure Blob support — resolved and captured in decisions.
- Embeddings: user chose to defer — resolved.

## Implementation notes

- Implement a `IFileStore` abstraction with two implementations: `LocalFileStore` and `AzureBlobFileStore`.
  - `IFileStore` responsibilities: `SaveFile(path, stream)`, `GetFile(path)`, `DeleteFile(path)`, `ListFiles(prefix)` and `GetPublicUrl(path)` (where supported).

- `Memory` entity stores metadata and a `ContentPath` pointing to the raw Markdown file in the `IFileStore`.

- Extraction pipeline steps:
  1. Fetch URL via HTTP client with timeout + error handling.
  2. Attempt Readability-like extraction to HTML -> convert to Markdown.
  3. If extraction appears empty or page requires JS (heuristics) -> render via Playwright and re-run extraction.
  4. Download images and persist via `IFileStore`; rewrite Markdown image links to local paths or accessible URLs.
  5. Persist `Memory` metadata and `ContentPath`.

- Provide API/UI endpoint to accept manual content (markdown/plaintext) and create `Memory` with `ExtractionStatus=manual`.

## Next steps

- Phase 1: produce `data-model.md`, `contracts/` (OpenAPI), `quickstart.md` and an initial implementation plan in `plan.md`.
