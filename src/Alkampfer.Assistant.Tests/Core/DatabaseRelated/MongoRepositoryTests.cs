using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.MongoDbIntegration;
using Alkampfer.Assistant.Interfaces;
using MongoDB.Driver;
using System;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.DatabaseRelated;

/// <summary>
/// Integration tests for MongoRepository.
/// These tests require a MongoDB instance to be running.
/// Set the TEST_MONGO_INSTANCE environment variable to specify the MongoDB connection string.
/// </summary>
[Trait("Category", "RequiresMongoDB")]
public class MongoRepositoryTests : RepositoryTestsBase
{
    private const string TestDatabaseName = "alkampfer_assistant_test";
    private const string TestCollectionName = "TestEntities";
    private const string DefaultConnectionString = "mongodb://localhost:27017";
    private static readonly Lazy<bool> MongoDbAvailable = new Lazy<bool>(CheckMongoDbAvailability);

    /// <summary>
    /// Constructor that checks MongoDB availability before running any tests.
    /// Throws an exception if MongoDB is not available, causing all tests to fail fast.
    /// </summary>
    public MongoRepositoryTests()
    {
        if (!MongoDbAvailable.Value)
        {
            throw new InvalidOperationException(
                "MongoDB is not available. Please ensure MongoDB is running and accessible at the configured connection string. " +
                $"Connection string: {GetTestConnectionString()}");
        }
    }

    /// <summary>
    /// Checks if MongoDB is available by attempting to connect and ping the server.
    /// Uses a 3-second timeout to fail fast if MongoDB is not available.
    /// </summary>
    /// <returns>True if MongoDB is available, false otherwise.</returns>
    private static bool CheckMongoDbAvailability()
    {
        try
        {
            var connectionString = GetTestConnectionString();
            
            // Configure MongoDB client with a 3-second timeout
            var mongoUrl = MongoUrl.Create(connectionString);
            var settings = MongoClientSettings.FromUrl(mongoUrl);
            settings.ConnectTimeout = TimeSpan.FromSeconds(3);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            
            var client = new MongoClient(settings);
            
            // Try to ping the server
            var database = client.GetDatabase(TestDatabaseName);
            database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
            
            return true;
        }
        catch (Exception)
        {
            // MongoDB is not available
            return false;
        }
    }

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

    protected override IRepository<TestEntity, TestEntityId> CreateRepository()
    {
        ClearTestCollection();
        var connectionString = GetTestConnectionString();
        return new MongoRepository<TestEntity, TestEntityId>(connectionString, TestDatabaseName, TestCollectionName);
    }

    // Note: These tests will only pass if MongoDB is available
    // Set the TEST_MONGO_INSTANCE environment variable to specify the MongoDB connection string
    // Example: TEST_MONGO_INSTANCE=mongodb://username:password@localhost:27017
    // The database name will always be set to 'alkampfer_assistant_test' regardless of the connection string
}
