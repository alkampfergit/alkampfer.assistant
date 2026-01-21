# Database Configuration Guide

This document explains how to configure the database for Alkampfer Assistant.

## Supported Databases

The application supports two database types:

1. **LiteDB** - File-based NoSQL database (default, no installation required)
2. **MongoDB** - Document database (requires MongoDB server)

## Configuration Priority

Configuration values are loaded in the following priority order (highest to lowest):

1. **Environment variables** with `ALKASS_` prefix (highest priority)
2. **alkampfer.assistant.json** override file
3. **appsettings.json** default configuration (lowest priority)

## Configuration Methods

### Method 1: appsettings.json (Default)

The default configuration is in `appsettings.json`:

```json
{
  "Database": {
    "Type": "LiteDb",
    "ConnectionString": "./data/assistant.db"
  }
}
```

### Method 2: Override File (alkampfer.assistant.json)

Create a file named `alkampfer.assistant.json` in the application directory or any parent directory. This file will override settings from `appsettings.json`.

**Example files provided:**
- `alkampfer.assistant.json.example` - LiteDB configuration
- `alkampfer.assistant.mongodb.json.example` - MongoDB configuration

**Using LiteDB:**
```json
{
  "Database": {
    "Type": "LiteDb",
    "ConnectionString": "./data/assistant.db"
  }
}
```

**Using MongoDB:**
```json
{
  "Database": {
    "Type": "MongoDb",
    "ConnectionString": "mongodb://localhost:27017/alkampfer_assistant"
  }
}
```

### Method 3: Environment Variables (Highest Priority)

Environment variables with the `ALKASS_` prefix override all other configuration sources.

**Syntax:** Use double underscores `__` to represent nested configuration keys.

**Examples:**

**Linux/macOS:**
```bash
# Use LiteDB
export ALKASS_Database__Type="LiteDb"
export ALKASS_Database__ConnectionString="./data/assistant.db"

# Use MongoDB
export ALKASS_Database__Type="MongoDb"
export ALKASS_Database__ConnectionString="mongodb://localhost:27017/alkampfer_assistant"
```

**Windows (PowerShell):**
```powershell
# Use LiteDB
$env:ALKASS_Database__Type="LiteDb"
$env:ALKASS_Database__ConnectionString="./data/assistant.db"

# Use MongoDB
$env:ALKASS_Database__Type="MongoDb"
$env:ALKASS_Database__ConnectionString="mongodb://localhost:27017/alkampfer_assistant"
```

**Windows (Command Prompt):**
```cmd
REM Use LiteDB
set ALKASS_Database__Type=LiteDb
set ALKASS_Database__ConnectionString=./data/assistant.db

REM Use MongoDB
set ALKASS_Database__Type=MongoDb
set ALKASS_Database__ConnectionString=mongodb://localhost:27017/alkampfer_assistant
```

**Docker/Docker Compose:**
```yaml
services:
  assistant:
    image: alkampfer/assistant
    environment:
      - ALKASS_Database__Type=MongoDb
      - ALKASS_Database__ConnectionString=mongodb://mongo:27017/alkampfer_assistant
    depends_on:
      - mongo

  mongo:
    image: mongo:latest
    ports:
      - "27017:27017"
```

## Database Configuration Properties

### Database.Type
- **Required:** Yes
- **Valid values:** `LiteDb` or `MongoDb`
- **Default:** `LiteDb`
- **Description:** Specifies which database system to use

### Database.ConnectionString
- **Required:** Yes
- **Format depends on Type:**
  - **LiteDb:** File path (absolute or relative)
    - Examples: `./data/assistant.db`, `C:/data/assistant.db`, `/var/data/assistant.db`
  - **MongoDb:** MongoDB connection string
    - Examples:
      - `mongodb://localhost:27017/alkampfer_assistant`
      - `mongodb://username:password@localhost:27017/alkampfer_assistant`
      - `mongodb+srv://cluster.mongodb.net/alkampfer_assistant`

## Database Collections/Tables

The application uses the following collections:

- **bookmarks** - Stores bookmark entities
- **memories** - Stores memory entities
- **counters** - System collection for ID generation

## Migration Between Databases

To migrate from LiteDB to MongoDB or vice versa:

1. Export data from the source database
2. Update the configuration to point to the new database
3. Import data into the target database

*Note: Migration tools are not currently included in the application.*

## Troubleshooting

### Error: "Database Type must be specified"
- Ensure the `Database.Type` configuration value is set
- Check that environment variables use the correct prefix `ALKASS_`

### Error: "Invalid database type"
- Verify that `Database.Type` is either `LiteDb` or `MongoDb` (case-insensitive)

### LiteDB: File not found errors
- The application will create the directory automatically if it doesn't exist
- Ensure the application has write permissions to the directory

### MongoDB: Connection errors
- Verify MongoDB server is running
- Check connection string format
- Ensure network access to MongoDB server
- Verify database name is included in connection string

## Security Considerations

### LiteDB
- Store the database file in a secure location
- Set appropriate file system permissions
- Regular backups recommended

### MongoDB
- Use authentication in production
- Use encrypted connections (TLS/SSL)
- Follow MongoDB security best practices
- Store connection strings securely (use environment variables, not config files)

## Examples

### Development with LiteDB (default)
No configuration needed - works out of the box.

### Production with MongoDB
```bash
export ALKASS_Database__Type="MongoDb"
export ALKASS_Database__ConnectionString="mongodb://user:pass@prod-mongo:27017/alkampfer_assistant"
```

### Testing with separate database
```bash
export ALKASS_Database__Type="LiteDb"
export ALKASS_Database__ConnectionString="./test-data/test.db"
```
