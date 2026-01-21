using System;

namespace Alkampfer.Assistant.Host.Configuration;

/// <summary>
/// Configuration for database persistence.
/// Supports both LiteDB (file-based) and MongoDB.
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// The type of database to use.
    /// Valid values: "LiteDb", "MongoDb"
    /// </summary>
    public string Type { get; set; } = "LiteDb";

    /// <summary>
    /// Connection string for the database.
    /// - For LiteDb: File path (e.g., "./data/assistant.db" or "C:/data/assistant.db")
    /// - For MongoDb: MongoDB connection string (e.g., "mongodb://localhost:27017/alkampfer_assistant")
    /// </summary>
    public string ConnectionString { get; set; } = "./data/assistant.db";

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            throw new InvalidOperationException("Database Type must be specified in configuration.");
        }

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("Database ConnectionString must be specified in configuration.");
        }

        var normalizedType = Type.ToLowerInvariant();
        if (normalizedType != "litedb" && normalizedType != "mongodb")
        {
            throw new InvalidOperationException(
                $"Invalid database type '{Type}'. Valid values are: 'LiteDb' or 'MongoDb'.");
        }

        // For LiteDb, ensure the directory exists
        if (normalizedType == "litedb")
        {
            var directory = Path.GetDirectoryName(ConnectionString);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <summary>
    /// Gets whether this is a LiteDB configuration.
    /// </summary>
    public bool IsLiteDb => Type.Equals("LiteDb", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this is a MongoDB configuration.
    /// </summary>
    public bool IsMongoDb => Type.Equals("MongoDb", StringComparison.OrdinalIgnoreCase);
}
