# Alkampfer.Assistant

Alkampfer.Assistant is a personal assistant application designed to help manage knowledge, tasks, and more.

## Features

### Bookmark Manager

The Bookmark Manager allows you to save, organize, and extract content from web pages for offline reading and archival.

#### Key Capabilities

- **Save Bookmarks**: Store URLs with titles and tags.
- **Content Extraction**: Automatically scrapes web pages and converts them to Markdown.
    - Uses **SmartReader** for article extraction.
    - Falls back to **Playwright** for JavaScript-heavy sites.
    - Downloads and stores images locally.
- **Manual Upload**: Upload text content manually if extraction fails or for non-web content.
- **Organization**: Filter bookmarks by tags.
- **Viewer**: Read extracted content directly in the application.

#### Setup & Configuration

1. **Prerequisites**:
   - .NET 9 SDK
   - Playwright browsers (required for scraping some sites):
     ```bash
     pwsh src/Alkampfer.Assistant.Host/bin/Debug/net9.0/playwright.ps1 install
     ```
     (Note: You may need to build the project first to generate the script)

2. **Configuration**:
   Update `src/Alkampfer.Assistant.Host/appsettings.json` to configure storage:

   ```json
   {
     "FileStore": {
       "Type": "Local", // or "AzureBlob"
       "BasePath": "/path/to/storage"
     },
     "Database": {
       "Path": "/path/to/litedb.db"
     }
   }
   ```

#### Usage

1. Run the Host application:
   ```bash
   dotnet run --project src/Alkampfer.Assistant.Host/Alkampfer.Assistant.Host.csproj
   ```
2. Open your browser to `https://localhost:7138` (or the configured port).
3. Navigate to the **Bookmarks** section in the sidebar.
4. Click **Add Bookmark** to save a new URL.
5. Click the **Download** icon on a bookmark to extract its content.
6. Click the **View** icon to read the extracted content.
