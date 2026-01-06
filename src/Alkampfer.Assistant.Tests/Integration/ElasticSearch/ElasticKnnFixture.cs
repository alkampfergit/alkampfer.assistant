using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.ElasticSearch;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Test fixture for KNN vector search tests with indexed vector data.
/// </summary>
public class ElasticKnnFixture : IAsyncLifetime
{
	public ElasticIndexer Indexer { get; private set; } = null!;
	public ElasticQueryExecutor QueryExecutor { get; private set; } = null!;
	public string TestIndexName { get; private set; } = null!;

	private readonly List<string> _indexesToCleanup = new();

	public async Task InitializeAsync()
	{
		DotEnv.Load();
		var elasticUrl = Environment.GetEnvironmentVariable("ELASTIC_TEST_URL");
		if (string.IsNullOrEmpty(elasticUrl))
		{
			throw new InvalidOperationException(
				"ELASTIC_TEST_URL environment variable is not set. " +
				"Please set it to your Elasticsearch instance URL (e.g., http://localhost:9200).");
		}

		var config = new ElasticSearchConfiguration
		{
			Address = elasticUrl,
			Username = null,
			Password = null,
			ShardNumber = 1,
			ReplicaNumber = 0,
			BulkBatchSize = 200
		};

		Indexer = new ElasticIndexer(config);
		QueryExecutor = new ElasticQueryExecutor(config);

		TestIndexName = $"test-knn-{Guid.NewGuid():N}";
		_indexesToCleanup.Add(TestIndexName);

		// Create index and register vector field
		await Indexer.EnsureIndexMappingAsync(TestIndexName);
		await Indexer.EnsureVectorFieldMappingAsync(
			TestIndexName,
			"embedding",
			dimensions: 3,
			similarity: "cosine",
			indexVectors: true);

		// Create test records with 3D vectors and various metadata
		var baseDate = new DateTime(2023, 1, 1);
		var records = new List<VectorRecord>
		{
			// Records with vectors at different positions
			VectorRecord.Create("v1", "doc-1")
				.WithText("Technology document about AI")
				.WithVector("embedding", new float[] { 1.0f, 0.0f, 0.0f })
				.WithMetadata("category", new[] { "technology" })
				.WithMetadata("rating", 4.5)
				.WithMetadata("year", 2023)
				.WithMetadata("created", baseDate),

			VectorRecord.Create("v2", "doc-2")
				.WithText("Another tech document")
				.WithVector("embedding", new float[] { 0.9f, 0.1f, 0.0f })
				.WithMetadata("category", new[] { "technology" })
				.WithMetadata("rating", 4.8)
				.WithMetadata("year", 2023)
				.WithMetadata("created", baseDate.AddDays(10)),

			VectorRecord.Create("v3", "doc-3")
				.WithText("Science document")
				.WithVector("embedding", new float[] { 0.0f, 1.0f, 0.0f })
				.WithMetadata("category", new[] { "science" })
				.WithMetadata("rating", 3.5)
				.WithMetadata("year", 2022)
				.WithMetadata("created", baseDate.AddDays(-30)),

			VectorRecord.Create("v4", "doc-4")
				.WithText("Arts and culture")
				.WithVector("embedding", new float[] { 0.0f, 0.0f, 1.0f })
				.WithMetadata("category", new[] { "arts" })
				.WithMetadata("rating", 4.2)
				.WithMetadata("year", 2021)
				.WithMetadata("created", baseDate.AddDays(-60)),

			VectorRecord.Create("v5", "doc-5")
				.WithText("Mixed content tech and science")
				.WithVector("embedding", new float[] { 0.5f, 0.5f, 0.0f })
				.WithMetadata("category", new[] { "technology", "science" })
				.WithMetadata("rating", 4.0)
				.WithMetadata("year", 2023)
				.WithMetadata("created", baseDate.AddDays(20)),

			VectorRecord.Create("v6", "doc-6")
				.WithText("Business document")
				.WithVector("embedding", new float[] { 0.3f, 0.3f, 0.4f })
				.WithMetadata("category", new[] { "business" })
				.WithMetadata("rating", 3.8)
				.WithMetadata("year", 2022)
				.WithMetadata("created", baseDate.AddDays(-10)),

			VectorRecord.Create("v7", "doc-7")
				.WithText("Technology review")
				.WithVector("embedding", new float[] { 0.8f, 0.2f, 0.0f })
				.WithMetadata("category", new[] { "technology" })
				.WithMetadata("rating", 5.0)
				.WithMetadata("year", 2023)
				.WithMetadata("created", baseDate.AddDays(30)),

			VectorRecord.Create("v8", "doc-8")
				.WithText("Historical document")
				.WithVector("embedding", new float[] { 0.1f, 0.1f, 0.8f })
				.WithMetadata("category", new[] { "history" })
				.WithMetadata("rating", 3.0)
				.WithMetadata("year", 2021)
				.WithMetadata("created", baseDate.AddDays(-90)),
		};

		await Indexer.IndexRecordsAsync(TestIndexName, records);
		// Refresh index to make documents immediately searchable
		await Indexer.RefreshIndexAsync(TestIndexName);
	}

	public async Task DisposeAsync()
	{
		foreach (var indexName in _indexesToCleanup)
		{
			try
			{
				await Indexer.DeleteIndexAsync(indexName);
			}
			catch
			{
				// Ignore cleanup errors
			}
		}
	}
}
