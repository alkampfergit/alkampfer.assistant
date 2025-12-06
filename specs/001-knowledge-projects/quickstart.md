# Quickstart — Bookmark Manager (dev)

Prerequisites

- .NET 9 SDK
- Node.js (for Playwright, if needed)

Local dev

1. Run tests:

```bash
dotnet test
```

2. Start the host (Blazor UI):

```bash
dotnet run --project src/Alkampfer.Assistant.Host
```

3. Default file store in dev is local filesystem. Configure file store in `appsettings.Development.json`:

```json
{
  "FileStore": {
    "Type": "Local",
    "BasePath": "./data/files"
  }
}
```

4. To enable Azure Blob storage, set:

```json
{
  "FileStore": {
    "Type": "AzureBlob",
    "ConnectionString": "<AZURE_CONN>",
    "Container": "bookmarks"
  }
}
```

API endpoints are documented in `specs/001-knowledge-projects/contracts/openapi.yaml`.
