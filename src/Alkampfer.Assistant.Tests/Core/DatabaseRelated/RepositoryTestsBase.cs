using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

public abstract class RepositoryTestsBase
{
    protected abstract IRepository<TestEntity> CreateRepository();

    [Fact]
    public async Task SaveAsync_WithValidEntity_ShouldSaveSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity
        {
            Id = "test-id-1",
            Name = "Test Entity",
            Value = 42
        };

        // Act
        await repository.SaveAsync(entity);

        // Assert
        var savedEntity = await repository.LoadByIdAsync("test-id-1");
        Assert.NotNull(savedEntity);
        Assert.Equal("test-id-1", savedEntity.Id);
        Assert.Equal("Test Entity", savedEntity.Name);
        Assert.Equal(42, savedEntity.Value);
    }

    [Fact]
    public async Task SaveAsync_WithNullOrEmptyId_ShouldThrowException()
    {
        // Arrange
        var repository = CreateRepository();
        var entityWithNullId = new TestEntity { Id = null!, Name = "Test" };
        var entityWithEmptyId = new TestEntity { Id = "", Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => repository.SaveAsync(entityWithNullId));
        await Assert.ThrowsAsync<Exception>(() => repository.SaveAsync(entityWithEmptyId));
    }

    [Fact]
    public async Task SaveAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Id = "test-id", Name = "Test" };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.SaveAsync(entity, cts.Token));
    }

    [Fact]
    public async Task LoadByIdAsync_WithExistingEntity_ShouldReturnEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity
        {
            Id = "existing-id",
            Name = "Existing Entity",
            Value = 123
        };

        await repository.SaveAsync(entity);

        // Act
        var loadedEntity = await repository.LoadByIdAsync("existing-id");

        // Assert
        Assert.NotNull(loadedEntity);
        Assert.Equal("existing-id", loadedEntity.Id);
        Assert.Equal("Existing Entity", loadedEntity.Name);
        Assert.Equal(123, loadedEntity.Value);
    }

    [Fact]
    public async Task LoadByIdAsync_WithNonExistentEntity_ShouldReturnNull()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var loadedEntity = await repository.LoadByIdAsync("non-existent-id");

        // Assert
        Assert.Null(loadedEntity);
    }

    [Fact]
    public async Task LoadByIdAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var repository = CreateRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.LoadByIdAsync("test-id", cts.Token));
    }

    [Fact]
    public async Task SaveAsync_UpdateExistingEntity_ShouldOverwriteEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var originalEntity = new TestEntity
        {
            Id = "update-test",
            Name = "Original Name",
            Value = 100
        };

        await repository.SaveAsync(originalEntity);

        var updatedEntity = new TestEntity
        {
            Id = "update-test",
            Name = "Updated Name",
            Value = 200
        };

        // Act
        await repository.SaveAsync(updatedEntity);

        // Assert
        var loadedEntity = await repository.LoadByIdAsync("update-test");
        Assert.NotNull(loadedEntity);
        Assert.Equal("update-test", loadedEntity.Id);
        Assert.Equal("Updated Name", loadedEntity.Name);
        Assert.Equal(200, loadedEntity.Value);
    }

    [Fact]
    public async Task Repository_WithMultipleEntities_ShouldMaintainSeparateData()
    {
        // Arrange
        var repository = CreateRepository();
        var entity1 = new TestEntity { Id = "entity-1", Name = "First Entity", Value = 1 };
        var entity2 = new TestEntity { Id = "entity-2", Name = "Second Entity", Value = 2 };
        var entity3 = new TestEntity { Id = "entity-3", Name = "Third Entity", Value = 3 };

        // Act
        await repository.SaveAsync(entity1);
        await repository.SaveAsync(entity2);
        await repository.SaveAsync(entity3);

        // Assert
        var loaded1 = await repository.LoadByIdAsync("entity-1");
        var loaded2 = await repository.LoadByIdAsync("entity-2");
        var loaded3 = await repository.LoadByIdAsync("entity-3");

        Assert.NotNull(loaded1);
        Assert.NotNull(loaded2);
        Assert.NotNull(loaded3);

        Assert.Equal("First Entity", loaded1.Name);
        Assert.Equal("Second Entity", loaded2.Name);
        Assert.Equal("Third Entity", loaded3.Name);

        Assert.Equal(1, loaded1.Value);
        Assert.Equal(2, loaded2.Value);
        Assert.Equal(3, loaded3.Value);
    }

    [Fact]
    public async Task Repository_WithSpecialCharactersInId_ShouldWorkCorrectly()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity
        {
            Id = "special-chars-!@#$%^&*()_+-=[]{}|;:,.<>?",
            Name = "Special ID Entity",
            Value = 999
        };

        // Act
        await repository.SaveAsync(entity);
        var loadedEntity = await repository.LoadByIdAsync("special-chars-!@#$%^&*()_+-=[]{}|;:,.<>?");

        // Assert
        Assert.NotNull(loadedEntity);
        Assert.Equal("special-chars-!@#$%^&*()_+-=[]{}|;:,.<>?", loadedEntity.Id);
        Assert.Equal("Special ID Entity", loadedEntity.Name);
        Assert.Equal(999, loadedEntity.Value);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_ShouldRemoveEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity
        {
            Id = "delete-test",
            Name = "Entity to Delete",
            Value = 100
        };

        await repository.SaveAsync(entity);
        
        // Verify entity exists
        var existingEntity = await repository.LoadByIdAsync("delete-test");
        Assert.NotNull(existingEntity);

        // Act
        await repository.DeleteAsync("delete-test");

        // Assert
        var deletedEntity = await repository.LoadByIdAsync("delete-test");
        Assert.Null(deletedEntity);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentEntity_ShouldNotThrow()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert - Should not throw for non-existent entity
        await repository.DeleteAsync("non-existent-id");
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var repository = CreateRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.DeleteAsync("test-id", cts.Token));
    }

    [Fact]
    public void AsQueryable_WithNoEntities_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var queryable = repository.AsQueryable;

        // Assert
        Assert.NotNull(queryable);
        Assert.Equal(0, queryable.Count());
    }

    [Fact]
    public async Task AsQueryable_WithEntities_ShouldReturnAllEntities()
    {
        // Arrange
        var repository = CreateRepository();
        var entity1 = new TestEntity { Id = "query-1", Name = "First", Value = 10 };
        var entity2 = new TestEntity { Id = "query-2", Name = "Second", Value = 20 };
        var entity3 = new TestEntity { Id = "query-3", Name = "Third", Value = 30 };

        await repository.SaveAsync(entity1);
        await repository.SaveAsync(entity2);
        await repository.SaveAsync(entity3);

        // Act
        var queryable = repository.AsQueryable;

        // Assert
        Assert.NotNull(queryable);
        Assert.Equal(3, queryable.Count());
        
        var entities = queryable.ToList();
        Assert.Contains(entities, e => e.Id == "query-1" && e.Name == "First" && e.Value == 10);
        Assert.Contains(entities, e => e.Id == "query-2" && e.Name == "Second" && e.Value == 20);
        Assert.Contains(entities, e => e.Id == "query-3" && e.Name == "Third" && e.Value == 30);
    }

    [Fact]
    public async Task AsQueryable_WithLinqQueries_ShouldSupportFiltering()
    {
        // Arrange
        var repository = CreateRepository();
        var entity1 = new TestEntity { Id = "filter-1", Name = "Apple", Value = 5 };
        var entity2 = new TestEntity { Id = "filter-2", Name = "Banana", Value = 15 };
        var entity3 = new TestEntity { Id = "filter-3", Name = "Cherry", Value = 25 };

        await repository.SaveAsync(entity1);
        await repository.SaveAsync(entity2);
        await repository.SaveAsync(entity3);

        // Act & Assert - Filter by Value
        var highValueEntities = repository.AsQueryable.Where(e => e.Value > 10).ToList();
        Assert.Equal(2, highValueEntities.Count);
        Assert.Contains(highValueEntities, e => e.Name == "Banana");
        Assert.Contains(highValueEntities, e => e.Name == "Cherry");

        // Act & Assert - Filter by Name
        var specificEntity = repository.AsQueryable.FirstOrDefault(e => e.Name == "Apple");
        Assert.NotNull(specificEntity);
        Assert.Equal("filter-1", specificEntity.Id);
        Assert.Equal(5, specificEntity.Value);
    }

    [Fact]
    public async Task AsQueryable_WithLinqQueries_ShouldSupportOrdering()
    {
        // Arrange
        var repository = CreateRepository();
        var entity1 = new TestEntity { Id = "order-1", Name = "Zebra", Value = 30 };
        var entity2 = new TestEntity { Id = "order-2", Name = "Apple", Value = 10 };
        var entity3 = new TestEntity { Id = "order-3", Name = "Banana", Value = 20 };

        await repository.SaveAsync(entity1);
        await repository.SaveAsync(entity2);
        await repository.SaveAsync(entity3);

        // Act & Assert - Order by Name
        var entitiesByName = repository.AsQueryable.OrderBy(e => e.Name).ToList();
        Assert.Equal(3, entitiesByName.Count);
        Assert.Equal("Apple", entitiesByName[0].Name);
        Assert.Equal("Banana", entitiesByName[1].Name);
        Assert.Equal("Zebra", entitiesByName[2].Name);

        // Act & Assert - Order by Value descending
        var entitiesByValueDesc = repository.AsQueryable.OrderByDescending(e => e.Value).ToList();
        Assert.Equal(3, entitiesByValueDesc.Count);
        Assert.Equal(30, entitiesByValueDesc[0].Value);
        Assert.Equal(20, entitiesByValueDesc[1].Value);
        Assert.Equal(10, entitiesByValueDesc[2].Value);
    }

    [Fact]
    public async Task AsQueryable_AfterDelete_ShouldReflectChanges()
    {
        // Arrange
        var repository = CreateRepository();
        var entity1 = new TestEntity { Id = "reflect-1", Name = "Keep", Value = 100 };
        var entity2 = new TestEntity { Id = "reflect-2", Name = "Delete", Value = 200 };

        await repository.SaveAsync(entity1);
        await repository.SaveAsync(entity2);

        // Verify both entities exist
        Assert.Equal(2, repository.AsQueryable.Count());

        // Act
        await repository.DeleteAsync("reflect-2");

        // Assert
        var queryable = repository.AsQueryable;
        Assert.Equal(1, queryable.Count());
        
        var remainingEntity = queryable.First();
        Assert.Equal("reflect-1", remainingEntity.Id);
        Assert.Equal("Keep", remainingEntity.Name);
    }

    [Fact]
    public async Task AsQueryable_AfterUpdate_ShouldReflectChanges()
    {
        // Arrange
        var repository = CreateRepository();
        var originalEntity = new TestEntity { Id = "update-query", Name = "Original", Value = 100 };
        await repository.SaveAsync(originalEntity);

        // Verify original state
        var beforeUpdate = repository.AsQueryable.First(e => e.Id == "update-query");
        Assert.Equal("Original", beforeUpdate.Name);
        Assert.Equal(100, beforeUpdate.Value);

        // Act - Update entity
        var updatedEntity = new TestEntity { Id = "update-query", Name = "Updated", Value = 200 };
        await repository.SaveAsync(updatedEntity);

        // Assert
        var afterUpdate = repository.AsQueryable.First(e => e.Id == "update-query");
        Assert.Equal("Updated", afterUpdate.Name);
        Assert.Equal(200, afterUpdate.Value);
    }

    [Fact]
    public async Task LoadAllAsync_WithNoEntities_ShouldReturnEmptyCollection()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var entities = await repository.LoadAllAsync();

        // Assert
        Assert.NotNull(entities);
        Assert.Empty(entities);
    }

    [Fact]
    public async Task LoadAllAsync_WithMultipleEntities_ShouldReturnAllEntities()
    {
        // Arrange
        var repository = CreateRepository();
        var entity1 = new TestEntity { Id = "all-1", Name = "First Entity", Value = 10 };
        var entity2 = new TestEntity { Id = "all-2", Name = "Second Entity", Value = 20 };
        var entity3 = new TestEntity { Id = "all-3", Name = "Third Entity", Value = 30 };

        await repository.SaveAsync(entity1);
        await repository.SaveAsync(entity2);
        await repository.SaveAsync(entity3);

        // Act
        var entities = await repository.LoadAllAsync();

        // Assert
        Assert.NotNull(entities);
        var entitiesList = entities.ToList();
        Assert.Equal(3, entitiesList.Count);
        
        Assert.Contains(entitiesList, e => e.Id == "all-1" && e.Name == "First Entity" && e.Value == 10);
        Assert.Contains(entitiesList, e => e.Id == "all-2" && e.Name == "Second Entity" && e.Value == 20);
        Assert.Contains(entitiesList, e => e.Id == "all-3" && e.Name == "Third Entity" && e.Value == 30);
    }

    [Fact]
    public async Task LoadAllAsync_AfterDeletion_ShouldReturnRemainingEntities()
    {
        // Arrange
        var repository = CreateRepository();
        var entity1 = new TestEntity { Id = "delete-all-1", Name = "Keep This", Value = 100 };
        var entity2 = new TestEntity { Id = "delete-all-2", Name = "Delete This", Value = 200 };
        var entity3 = new TestEntity { Id = "delete-all-3", Name = "Keep This Too", Value = 300 };

        await repository.SaveAsync(entity1);
        await repository.SaveAsync(entity2);
        await repository.SaveAsync(entity3);

        // Act
        await repository.DeleteAsync("delete-all-2");
        var entities = await repository.LoadAllAsync();

        // Assert
        Assert.NotNull(entities);
        var entitiesList = entities.ToList();
        Assert.Equal(2, entitiesList.Count);
        
        Assert.Contains(entitiesList, e => e.Id == "delete-all-1");
        Assert.Contains(entitiesList, e => e.Id == "delete-all-3");
        Assert.DoesNotContain(entitiesList, e => e.Id == "delete-all-2");
    }

    [Fact]
    public async Task LoadAllAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var repository = CreateRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.LoadAllAsync(cts.Token));
    }
}

public class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}