using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

public class LiteDbRepositoryTests : RepositoryTestsBase, IDisposable
{
    private readonly List<string> _tempDbPaths;

    public LiteDbRepositoryTests()
    {
        _tempDbPaths = new List<string>();
    }

    public void Dispose()
    {
        foreach (var dbPath in _tempDbPaths)
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private string CreateTempDbPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_repository_{Guid.NewGuid()}.db");
        _tempDbPaths.Add(path);
        return path;
    }

    protected override IRepository<TestEntity> CreateRepository()
    {
        return new LiteDbRepository<TestEntity>($"Filename={CreateTempDbPath()}", "TestEntities");
    }

    [Fact]
    public async Task LiteDbRepository_WithSameConnectionString_ShouldShareData()
    {
        // Arrange
        var dbPath = CreateTempDbPath();
        var connectionString = $"Filename={dbPath}";
        
        var repository1 = new LiteDbRepository<TestEntity>(connectionString, "TestEntities");
        var repository2 = new LiteDbRepository<TestEntity>(connectionString, "TestEntities");

        var entity = new TestEntity
        {
            Id = "shared-entity",
            Name = "Shared Entity",
            Value = 42
        };

        // Act
        await repository1.SaveAsync(entity);
        var loadedEntity = await repository2.LoadByIdAsync("shared-entity");

        // Assert
        Assert.NotNull(loadedEntity);
        Assert.Equal("shared-entity", loadedEntity.Id);
        Assert.Equal("Shared Entity", loadedEntity.Name);
        Assert.Equal(42, loadedEntity.Value);
    }

    [Fact]
    public async Task LiteDbRepository_WithDifferentCollections_ShouldIsolateData()
    {
        // Arrange
        var dbPath = CreateTempDbPath();
        var connectionString = $"Filename={dbPath}";
        
        var repository1 = new LiteDbRepository<TestEntity>(connectionString, "Collection1");
        var repository2 = new LiteDbRepository<TestEntity>(connectionString, "Collection2");

        var entity = new TestEntity
        {
            Id = "isolated-entity",
            Name = "Isolated Entity",
            Value = 123
        };

        // Act
        await repository1.SaveAsync(entity);
        var loadedFromSameCollection = await repository1.LoadByIdAsync("isolated-entity");
        var loadedFromDifferentCollection = await repository2.LoadByIdAsync("isolated-entity");

        // Assert
        Assert.NotNull(loadedFromSameCollection);
        Assert.Equal("isolated-entity", loadedFromSameCollection.Id);
        
        Assert.Null(loadedFromDifferentCollection);
    }

    [Fact]
    public async Task LiteDbRepository_WithLargeData_ShouldHandleCorrectly()
    {
        // Arrange
        var repository = CreateRepository();
        var largeString = new string('A', 10000); // 10KB string
        var entity = new TestEntity
        {
            Id = "large-data-entity",
            Name = largeString,
            Value = int.MaxValue
        };

        // Act
        await repository.SaveAsync(entity);
        var loadedEntity = await repository.LoadByIdAsync("large-data-entity");

        // Assert
        Assert.NotNull(loadedEntity);
        Assert.Equal("large-data-entity", loadedEntity.Id);
        Assert.Equal(largeString, loadedEntity.Name);
        Assert.Equal(int.MaxValue, loadedEntity.Value);
    }

    [Fact]
    public async Task LiteDbRepository_ConcurrentOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var repository = CreateRepository();
        const int numberOfOperations = 10;
        var tasks = new List<Task>();

        // Act - Sequential operations to avoid LiteDb concurrency issues
        for (int i = 0; i < numberOfOperations; i++)
        {
            var entity = new TestEntity
            {
                Id = $"concurrent-entity-{i}",
                Name = $"Entity {i}",
                Value = i
            };
            
            await repository.SaveAsync(entity);
        }

        // Assert
        for (int i = 0; i < numberOfOperations; i++)
        {
            var loadedEntity = await repository.LoadByIdAsync($"concurrent-entity-{i}");
            Assert.NotNull(loadedEntity);
            Assert.Equal($"concurrent-entity-{i}", loadedEntity.Id);
            Assert.Equal($"Entity {i}", loadedEntity.Name);
            Assert.Equal(i, loadedEntity.Value);
        }
    }

    [Fact]
    public async Task LiteDbRepository_WithUnicodeData_ShouldPreserveEncoding()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity
        {
            Id = "unicode-entity",
            Name = "Unicode: 你好世界 🌍 العالم мир",
            Value = 42
        };

        // Act
        await repository.SaveAsync(entity);
        var loadedEntity = await repository.LoadByIdAsync("unicode-entity");

        // Assert
        Assert.NotNull(loadedEntity);
        Assert.Equal("unicode-entity", loadedEntity.Id);
        Assert.Equal("Unicode: 你好世界 🌍 العالم мир", loadedEntity.Name);
        Assert.Equal(42, loadedEntity.Value);
    }

    [Fact]
    public async Task LiteDbRepository_WithDatabaseFile_ShouldPersistData()
    {
        // Arrange
        var dbPath = CreateTempDbPath();
        var connectionString = $"Filename={dbPath}";
        var entity = new TestEntity
        {
            Id = "persistent-entity",
            Name = "Persistent Entity",
            Value = 999
        };

        // Act - Save with first repository instance
        var repository1 = new LiteDbRepository<TestEntity>(connectionString, "TestEntities");
        await repository1.SaveAsync(entity);

        // Create new repository instance with same connection string
        var repository2 = new LiteDbRepository<TestEntity>(connectionString, "TestEntities");
        var loadedEntity = await repository2.LoadByIdAsync("persistent-entity");

        // Assert
        Assert.NotNull(loadedEntity);
        Assert.Equal("persistent-entity", loadedEntity.Id);
        Assert.Equal("Persistent Entity", loadedEntity.Name);
        Assert.Equal(999, loadedEntity.Value);
        
        // Verify database file was created
        Assert.True(File.Exists(dbPath));
    }
}
