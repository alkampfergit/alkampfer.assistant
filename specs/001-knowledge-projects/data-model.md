# Data Model: Bookmark Manager

## Entities

### Memory (generic, reusable)

A generic entity for storing Markdown content. Memory has no metadata of its own — metadata is provided by the entity that references it (e.g., Bookmark).

- id: `MemoryId` (subclass of `IDentity` class)
- content_path: string (path in `IFileStore` pointing to a Markdown file)
- attachments: array of { name, path } (downloaded images/files referenced by the Markdown)
- created_at: datetime
- updated_at: datetime

### Bookmark

A resource representing a URL. Bookmark owns the metadata and optionally references a Memory for downloaded content.

- id: `BookmarkId` (subclass of `IDentity` class)
- title: string
- url: string
- tags: array of string
- status: enum { new, downloaded, error }
- last_error: string (nullable)
- memory_id: `MemoryId` (FK -> Memory.id, nullable — set when content is downloaded or manually inserted)
- created_at: datetime
- updated_at: datetime

## Notes

- Bookmark owns all metadata (title, url, tags, status).
- Memory is a generic Markdown store; it knows nothing about the entity that uses it.
- When content is extracted or manually inserted, a Memory record is created and `Bookmark.memory_id` is set.
- Memory can be reused by other entity types in future iterations (e.g., notes, documents).
- `Memory.content_path` points to the stored Markdown file in the file store abstraction.
- Consider indexing `tags` and `memory_id` for efficient queries.
