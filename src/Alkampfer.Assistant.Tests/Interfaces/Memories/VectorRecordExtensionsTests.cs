using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Alkampfer.Assistant.ElasticSearch;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Interfaces.Memories
{
    public class VectorRecordExtensionsTests
    {
        [Fact]
        public void ToExpandoObject_IncludesCoreFieldsAndPrefixedMetadata_ForAllTypes()
        {
            var dt = DateTime.UtcNow;
            var record = new VectorRecordTestHelpers.VectorRecordBuilder("id-1", "doc-1")
                .WithString("title", "hello")
                .WithInt("count", 42)
                .WithDouble("score", 3.14)
                .WithBool("flag", true)
                .WithDate("created", dt)
                .Build();

            var obj = record.ToExpandoObjectForIndexing();

            Assert.Equal("id-1", obj["id"]);
            Assert.Equal("doc-1", obj["documentId"]);
            Assert.Equal("hello", obj["s_title"]);
            Assert.Equal(42, obj["i_count"]);
            Assert.Equal(3.14, (double)obj["n_score"], 9);
            Assert.Equal(true, obj["b_flag"]);
            var expectedDate = dt.ToString("O", CultureInfo.InvariantCulture);
            Assert.Equal(expectedDate, obj["d_created"]);
        }

        [Fact]
        public void ToExpandoObject_WithNoMetadata_ReturnsOnlyCoreFields()
        {
            var record = VectorRecord.Create("id-2", "doc-2");
            var obj = record.ToExpandoObjectForIndexing();

            Assert.Equal(2, obj.Count);
            Assert.Equal("id-2", obj["id"]);
            Assert.Equal("doc-2", obj["documentId"]);
        }

        [Fact]
        public void ToExpandoObject_WithText_IncludesTextField()
        {
            var record = VectorRecord.Create("id-text-1", "doc-text-1")
                .WithText("This is a sample text content");

            var obj = record.ToExpandoObjectForIndexing();

            Assert.Equal(3, obj.Count); // id, documentId, text
            Assert.Equal("id-text-1", obj["id"]);
            Assert.Equal("doc-text-1", obj["documentId"]);
            Assert.Equal("This is a sample text content", obj["text"]);
        }

        [Fact]
        public void ToExpandoObject_WithNullText_ExcludesTextField()
        {
            var record = VectorRecord.Create("id-text-2", "doc-text-2")
                .WithText(null);

            var obj = record.ToExpandoObjectForIndexing();

            Assert.Equal(2, obj.Count); // Only id and documentId
            Assert.False(obj.ContainsKey("text"));
        }

        [Fact]
        public void ToExpandoObject_WithTextAndMetadata_IncludesBoth()
        {
            var record = VectorRecord.Create("id-text-3", "doc-text-3")
                .WithText("Sample text")
                .WithMetadata("category", "test")
                .WithMetadata("count", 10);

            var obj = record.ToExpandoObjectForIndexing();

            Assert.Equal(5, obj.Count); // id, documentId, text, s_category, i_count
            Assert.Equal("Sample text", obj["text"]);
            Assert.Equal("test", obj["s_category"]);
            Assert.Equal(10, obj["i_count"]);
        }

        [Fact]
        public void FromJsonElement_Reconstructs_AllMetadataTypes()
        {
            var dt = DateTime.UtcNow;
            var payload = new Dictionary<string, object>
            {
                { "id", "id-3" },
                { "documentId", "doc-3" },
                { "s_title", "hello" },
                { "i_count", 42 },
                { "n_score", 2.71828 },
                { "b_flag", true },
                { "d_created", dt.ToString("O", CultureInfo.InvariantCulture) }
            };

            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Equal("id-3", record.Id);
            Assert.Equal("doc-3", record.DocumentId);
            Assert.Equal("hello", record.GetMetadataAsString("title"));
            Assert.Equal(42, record.GetMetadataAsInt("count"));
            Assert.True(Math.Abs(record.GetMetadataAsDouble("score")!.Value - 2.71828) < 1e-9);
            Assert.Equal(true, record.GetMetadataAsBool("flag"));
            var parsed = record.GetMetadataAsDateTime("created");
            Assert.NotNull(parsed);
            Assert.True(Math.Abs((parsed.Value - dt).TotalSeconds) < 1);
        }

        [Fact]
        public void FromJsonElement_MissingId_ThrowsInvalidOperationException()
        {
            var payload = new Dictionary<string, object>
            {
                { "documentId", "doc-4" }
            };

            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            Assert.Throws<InvalidOperationException>(() => VectorRecordExtensions.FromJsonElement(json));
        }

        [Fact]
        public void FromJsonElement_MissingDocumentId_ThrowsInvalidOperationException()
        {
            var payload = new Dictionary<string, object>
            {
                { "id", "id-5" }
            };

            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            Assert.Throws<InvalidOperationException>(() => VectorRecordExtensions.FromJsonElement(json));
        }

        [Fact]
        public void FromJsonElement_NullFieldValues_AreIgnored()
        {
            var payload = "{ \"id\": \"id-6\", \"documentId\": \"doc-6\", \"s_title\": null, \"i_count\": null }";
            var json = VectorRecordTestHelpers.JsonElementFromRawString(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Equal("id-6", record.Id);
            Assert.Equal("doc-6", record.DocumentId);
            Assert.Null(record.GetMetadataAsString("title"));
            Assert.Null(record.GetMetadataAsInt("count"));
        }

        [Fact]
        public void FromJsonElement_UnknownPrefixes_Ignored()
        {
            var payload = new Dictionary<string, object>
            {
                { "id", "id-7" },
                { "documentId", "doc-7" },
                { "x_unknown", "value" }
            };
            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Null(record.GetMetadataAsString("unknown"));
        }

        [Fact]
        public void FromJsonElement_InvalidTypeValues_AreIgnored()
        {
            var payload = "{ \"id\": \"id-8\", \"documentId\": \"doc-8\", \"i_count\": \"not-an-int\", \"n_score\": \"not-a-number\", \"b_flag\": \"true\" }";
            var json = VectorRecordTestHelpers.JsonElementFromRawString(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Null(record.GetMetadataAsInt("count"));
            Assert.Null(record.GetMetadataAsDouble("score"));
            Assert.Null(record.GetMetadataAsBool("flag"));
        }

        [Fact]
        public void FromJsonElement_DateParsing_RespectsRoundtripFormatAndIgnoresInvalid()
        {
            var dt = DateTime.UtcNow;
            var payload = new Dictionary<string, object>
            {
                { "id", "id-9" },
                { "documentId", "doc-9" },
                { "d_ok", dt.ToString("O", CultureInfo.InvariantCulture) },
                { "d_bad", "not-a-date" }
            };

            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            var ok = record.GetMetadataAsDateTime("ok");
            var bad = record.GetMetadataAsDateTime("bad");

            Assert.NotNull(ok);
            Assert.True(Math.Abs((ok.Value - dt).TotalSeconds) < 1);
            Assert.Null(bad);
        }

        [Fact]
        public void RoundTrip_ToExpando_ToJson_FromJson_PreservesMetadata()
        {
            var dt = DateTime.UtcNow;
            var original = new VectorRecordTestHelpers.VectorRecordBuilder("id-10", "doc-10")
                .WithString("title", "round")
                .WithInt("count", 7)
                .WithDouble("score", 1.2345)
                .WithBool("flag", false)
                .WithDate("created", dt)
                .Build();

            var expando = original.ToExpandoObjectForIndexing();
            var json = JsonSerializer.Serialize(expando);
            using var doc = JsonDocument.Parse(json);
            var parsed = VectorRecordExtensions.FromJsonElement(doc.RootElement);

            Assert.Equal(original.Id, parsed.Id);
            Assert.Equal(original.DocumentId, parsed.DocumentId);
            Assert.Equal(original.GetMetadataAsString("title"), parsed.GetMetadataAsString("title"));
            Assert.Equal(original.GetMetadataAsInt("count"), parsed.GetMetadataAsInt("count"));
            Assert.True(Math.Abs(original.GetMetadataAsDouble("score")!.Value - parsed.GetMetadataAsDouble("score")!.Value) < 1e-9);
            Assert.Equal(original.GetMetadataAsBool("flag"), parsed.GetMetadataAsBool("flag"));
            Assert.True(Math.Abs((original.GetMetadataAsDateTime("created")!.Value - parsed.GetMetadataAsDateTime("created")!.Value).TotalSeconds) < 1);
        }

        [Fact]
        public void FromJsonElement_IntOverflow_IsIgnored()
        {
            var payload = "{ \"id\": \"id-11\", \"documentId\": \"doc-11\", \"i_big\": 9223372036854775807 }"; // larger than int32
            var json = VectorRecordTestHelpers.JsonElementFromRawString(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Null(record.GetMetadataAsInt("big"));
        }

        [Fact]
        public void FromJsonElement_WithTextField_ReconstructsText()
        {
            var payload = new Dictionary<string, object>
            {
                { "id", "id-text-4" },
                { "documentId", "doc-text-4" },
                { "text", "This is the text content" }
            };

            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Equal("id-text-4", record.Id);
            Assert.Equal("doc-text-4", record.DocumentId);
            Assert.Equal("This is the text content", record.Text);
        }

        [Fact]
        public void FromJsonElement_WithoutTextField_TextIsNull()
        {
            var payload = new Dictionary<string, object>
            {
                { "id", "id-text-5" },
                { "documentId", "doc-text-5" }
            };

            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Null(record.Text);
        }

        [Fact]
        public void FromJsonElement_WithNullTextField_TextIsNull()
        {
            var payload = "{ \"id\": \"id-text-6\", \"documentId\": \"doc-text-6\", \"text\": null }";
            var json = VectorRecordTestHelpers.JsonElementFromRawString(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Null(record.Text);
        }

        [Fact]
        public void FromJsonElement_WithTextAndMetadata_ReconstructsBoth()
        {
            var payload = new Dictionary<string, object>
            {
                { "id", "id-text-7" },
                { "documentId", "doc-text-7" },
                { "text", "Combined content" },
                { "s_category", "test" },
                { "i_count", 42 }
            };

            var json = VectorRecordTestHelpers.JsonElementFromObject(payload);
            var record = VectorRecordExtensions.FromJsonElement(json);

            Assert.Equal("Combined content", record.Text);
            Assert.Equal("test", record.GetMetadataAsString("category"));
            Assert.Equal(42, record.GetMetadataAsInt("count"));
        }

        [Fact]
        public void RoundTrip_WithText_PreservesTextAndMetadata()
        {
            var original = VectorRecord.Create("id-text-8", "doc-text-8")
                .WithText("Round trip text content")
                .WithMetadata("title", "Test")
                .WithMetadata("count", 100);

            var expando = original.ToExpandoObjectForIndexing();
            var json = JsonSerializer.Serialize(expando);
            using var doc = JsonDocument.Parse(json);
            var parsed = VectorRecordExtensions.FromJsonElement(doc.RootElement);

            Assert.Equal(original.Id, parsed.Id);
            Assert.Equal(original.DocumentId, parsed.DocumentId);
            Assert.Equal(original.Text, parsed.Text);
            Assert.Equal(original.GetMetadataAsString("title"), parsed.GetMetadataAsString("title"));
            Assert.Equal(original.GetMetadataAsInt("count"), parsed.GetMetadataAsInt("count"));
        }
    }
}
