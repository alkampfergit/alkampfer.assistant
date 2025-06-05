using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.MongoDbIntegration;
using MongoDB.Driver;
using System;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

/// <summary>
/// Integration tests for MongoRepository.
/// These tests require a MongoDB instance to be running.
/// Set the TEST_MONGO_INSTANCE environment variable to specify the MongoDB connection string.
/// </summary>
public class MongoRepositoryTests : RepositoryTestsBase
{
    private const string TestDatabaseName = "alkampfer_assistant_test";
    private const string TestCollectionName = "TestEntities";
    private const string DefaultConnectionString = "mongodb://localhost:27017";

    /// <summary>
    /// Gets the MongoDB connection string from environment variable or uses default.
    /// Ensures the connection always uses the test database.
    /// </summary>
    private static string GetTestConnectionString()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("TEST_MONGO_INSTANCE") ?? DefaultConnectionString;
        
        // Parse the connection string and ensure we use the test database
        var builder = new MongoUrlBuilder(baseConnectionString)
        {
            DatabaseName = TestDatabaseName
        };
        
        return builder.ToMongoUrl().ToString();
    }

    /// <summary>
    /// Removes all documents from the test collection.
    /// </summary>
    private static void ClearTestCollection()
    {
        var connectionString = GetTestConnectionString();
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(TestDatabaseName);
        var collection = database.GetCollection<TestEntity>(TestCollectionName);
        collection.DeleteMany(FilterDefinition<TestEntity>.Empty);
    }

    protected override IRepository<TestEntity> CreateRepository()
    {
        ClearTestCollection();
        var connectionString = GetTestConnectionString();
        return new MongoRepository<TestEntity>(connectionString, TestDatabaseName, TestCollectionName);
    }

    // Note: These tests will only pass if MongoDB is available
    // Set the TEST_MONGO_INSTANCE environment variable to specify the MongoDB connection string
    // Example: TEST_MONGO_INSTANCE=mongodb://username:password@localhost:27017
    // The database name will always be set to 'alkampfer.assistant.test' regardless of the connection string
}
