using System;
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
        var entityWithNullId = new TestEntity { Id = null, Name = "Test" };
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
}

// Test entity class for repository testing
public class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
