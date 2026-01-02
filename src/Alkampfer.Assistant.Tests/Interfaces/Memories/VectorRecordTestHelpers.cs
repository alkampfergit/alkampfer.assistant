using System;
using System.Collections.Generic;
using System.Text.Json;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.Tests.Interfaces.Memories
{
    internal static class VectorRecordTestHelpers
    {
        public static JsonElement JsonElementFromObject(object obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = null });
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        public static JsonElement JsonElementFromRawString(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        public sealed class VectorRecordBuilder
        {
            private readonly string _id;
            private readonly string _documentId;
            private readonly VectorRecord _record;

            public VectorRecordBuilder(string id, string documentId)
            {
                _id = id;
                _documentId = documentId;
                _record = VectorRecord.Create(id, documentId);
            }

            public VectorRecordBuilder WithString(string key, string value)
            {
                _record.WithMetadata(key, value);
                return this;
            }

            public VectorRecordBuilder WithInt(string key, int value)
            {
                _record.WithMetadata(key, value);
                return this;
            }

            public VectorRecordBuilder WithDouble(string key, double value)
            {
                _record.WithMetadata(key, value);
                return this;
            }

            public VectorRecordBuilder WithBool(string key, bool value)
            {
                _record.WithMetadata(key, value);
                return this;
            }

            public VectorRecordBuilder WithDate(string key, DateTime value)
            {
                _record.WithMetadata(key, value);
                return this;
            }

            public VectorRecordBuilder WithKeywords(string key, string[] value)
            {
                _record.WithMetadata(key, value);
                return this;
            }

            public VectorRecord Build() => _record;
        }

        public static bool AreExpandoDictionariesEqual(IDictionary<string, object> expected, IDictionary<string, object> actual, out string? message)
        {
            if (expected.Count != actual.Count)
            {
                message = $"Different key count: expected {expected.Count}, actual {actual.Count}";
                return false;
            }

            foreach (var kv in expected)
            {
                if (!actual.TryGetValue(kv.Key, out var val))
                {
                    message = $"Missing key '{kv.Key}' in actual dictionary.";
                    return false;
                }

                if (!AreValuesEqual(kv.Value, val))
                {
                    message = $"Value mismatch for key '{kv.Key}': expected '{kv.Value}' ({kv.Value?.GetType().Name}), actual '{val}' ({val?.GetType().Name}).";
                    return false;
                }
            }

            message = null;
            return true;
        }

        private static bool AreValuesEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            if (a is double da && b is double db) return Math.Abs(da - db) < 1e-9;
            if (a is float fa && b is float fb) return Math.Abs(fa - fb) < 1e-6;
            if (a is DateTime daTime && b is string bStr)
            {
                if (DateTime.TryParse(bStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    return Math.Abs((parsed - daTime).TotalSeconds) < 1;
            }

            return a.Equals(b);
        }
    }
}
