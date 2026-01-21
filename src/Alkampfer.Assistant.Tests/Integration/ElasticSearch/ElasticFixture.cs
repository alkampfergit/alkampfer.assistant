using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.ElasticSearch;
using Alkampfer.Assistant.Interfaces.Memories;
using static Alkampfer.Assistant.Tests.Integration.ElasticSearch.TestIndexUtils;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

public class ElasticFixture : IAsyncLifetime
{
	public ElasticIndexer Indexer { get; private set; } = null!;
	public ElasticQueryExecutor QueryExecutor { get; private set; } = null!;
	public string TestIndexName { get; private set; } = null!;
	public string EmptyIndexName { get; private set; } = null!;

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

		TestIndexName = $"aatest-query-{Guid.NewGuid():N}";
		EmptyIndexName = $"{TestIndexName}-empty";

		// Validate that the test index follows the test naming convention
		AssertIsTestIndexName(TestIndexName);

		_indexesToCleanup.Add(TestIndexName);
		_indexesToCleanup.Add(EmptyIndexName);

		await Indexer.EnsureIndexMappingAsync(TestIndexName);

		var baseDate = new DateTime(2023, 1, 1);
		var records = new List<VectorRecord>
		{
			VectorRecord.Create("r1", "doc-elastic-1").WithText("This is a test document about Elasticsearch").WithMetadata("category", "technology"),
			VectorRecord.Create("r2", "doc-elastic-2").WithText("Another document about databases").WithMetadata("category", "technology"),
			VectorRecord.Create("r3", "doc-elastic-3").WithText("Something completely different").WithMetadata("category", "other"),

			VectorRecord.Create("t1", "doc-title-1").WithText("Document 1").WithMetadata("title", "First Title"),
			VectorRecord.Create("t2", "doc-title-2").WithText("Document 2").WithMetadata("title", "Second Title"),
			VectorRecord.Create("t3", "doc-title-3").WithText("Document 3").WithMetadata("title", "Third Title"),

			VectorRecord.Create("y1", "doc-year-1").WithText("Document 1").WithMetadata("year", 2021),
			VectorRecord.Create("y2", "doc-year-2").WithText("Document 2").WithMetadata("year", 2022),
			VectorRecord.Create("y3", "doc-year-3").WithText("Document 3").WithMetadata("year", 2023),

			VectorRecord.Create("s1", "doc-status-1").WithText("Document 1").WithMetadata("status", new[] { "Active" }),
			VectorRecord.Create("s2", "doc-status-2").WithText("Document 2").WithMetadata("status", new[] { "Inactive" }),
			VectorRecord.Create("s3", "doc-status-3").WithText("Document 3").WithMetadata("status", new[] { "Active" }),

			VectorRecord.Create("d1", "doc-date-1").WithText("Document 1").WithMetadata("created", baseDate.AddDays(-10)),
			VectorRecord.Create("d2", "doc-date-2").WithText("Document 2").WithMetadata("created", baseDate),
			VectorRecord.Create("d3", "doc-date-3").WithText("Document 3").WithMetadata("created", baseDate.AddDays(10)),

			VectorRecord.Create("m1", "doc-multi-1").WithText("Document 1").WithMetadata("category", "tech").WithMetadata("year", 2021),
			VectorRecord.Create("m2", "doc-multi-2").WithText("Document 2").WithMetadata("category", "tech").WithMetadata("year", 2022),
			VectorRecord.Create("m3", "doc-multi-3").WithText("Document 3").WithMetadata("category", "other").WithMetadata("year", 2022)
		};

		records.AddRange(Enumerable.Range(1, 20).Select(i =>
			VectorRecord.Create($"p{i}", $"doc-paging-{i}").WithText($"Document {i}")));

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
