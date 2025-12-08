using System;
using System.IO;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Configuration;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using LiteDB;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

/// <summary>
/// Tests to verify that Identity types are persisted as strings in LiteDB.
/// </summary>
public class IdentitySerializationTests : IDisposable
{
    private readonly string _dbPath;

    public IdentitySerializationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_identity_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task LiteDb_ShouldPersistIdentityAsString()
    {
        // Arrange
        var connectionString = $"Filename={_dbPath}";
        var repository = new LiteDbRepository<ModelDefinition, ModelDefinitionId>(connectionString, "models");
        var entity = new ModelDefinition
        {
            Id = new ModelDefinitionId(42),
            Url = "https://example.com",
            ApiKey = "test-key",
            Models = []
        };

        // Act - Save the entity
        await repository.SaveAsync(entity);

        // Assert - Open the database directly and verify the ID is stored as a string
        using var db = new LiteDatabase(connectionString);
        var collection = db.GetCollection("models");
        var doc = collection.FindAll().First();
        
        // The _id field should be a string with format "ModelDefinition/42"
        var idValue = doc["_id"];
        Assert.Equal(BsonType.String, idValue.Type);
        Assert.Equal("ModelDefinition/42", idValue.AsString);
    }

    [Fact]
    public async Task LiteDb_ShouldDeserializeStringToIdentity()
    {
        // Arrange
        var connectionString = $"Filename={_dbPath}";
        var repository = new LiteDbRepository<ModelDefinition, ModelDefinitionId>(connectionString, "models");
        var originalId = new ModelDefinitionId(123);
        var entity = new ModelDefinition
        {
            Id = originalId,
            Url = "https://example.com",
            ApiKey = "test-key",
            Models = []
        };

        // Act - Save and reload
        await repository.SaveAsync(entity);
        var loaded = await repository.LoadByIdAsync(new ModelDefinitionId(123));

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(originalId, loaded.Id);
        Assert.Equal("ModelDefinition/123", loaded.Id.Value);
        Assert.Equal(123, loaded.Id.NumericId);
    }
}
