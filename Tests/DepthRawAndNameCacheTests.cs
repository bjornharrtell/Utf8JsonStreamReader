using System.Text;
using System.Text.Json;
using Wololo.Text.Json;

namespace Tests;

[TestClass]
public class DepthRawAndNameCacheTests
{
    static MemoryStream Utf8(string json) => new(Encoding.UTF8.GetBytes(json));

    // Mimics a DAF file: a root array of records that have scalar and nested properties
    const string Records = """
        [
          { "id": 1, "name": "a\u00e6", "geo": { "type": "Point", "coordinates": [1.5, 2, [3]] }, "tags": ["x", {"y": null}], "n": null },
          { "id": 2, "name": "b", "geo": {}, "tags": [], "n": true }
        ]
        """;

    static string Records_Raw(int record, string property)
    {
        using var doc = JsonDocument.Parse(Records);
        return doc.RootElement[record].GetProperty(property).GetRawText();
    }

    // ---- Depth ----

    [TestMethod]
    public void DepthMatchesUtf8JsonReaderCurrentDepth()
    {
        var expected = new List<(JsonTokenType, int)>();
        new Utf8JsonStreamReader().Read(
            Utf8(Records),
            (ref Utf8JsonReader r) => expected.Add((r.TokenType, r.CurrentDepth))
        );

        var actual = new Utf8JsonStreamReader()
            .ToEnumerable(Utf8(Records))
            .Select(r => (r.TokenType, r.Depth))
            .ToList();

        CollectionAssert.AreEqual(expected, actual);
        Assert.AreEqual(0, actual[0].Item2); // root StartArray
        Assert.AreEqual(1, actual[1].Item2); // record StartObject
        Assert.AreEqual(2, actual[2].Item2); // first property name
    }

    [TestMethod]
    public async Task DepthAsyncMatchesSync()
    {
        var sync = new Utf8JsonStreamReader(16)
            .ToEnumerable(Utf8(Records))
            .Select(r => (r.TokenType, r.Depth))
            .ToList();
        var async = new List<(JsonTokenType, int)>();
        await foreach (var r in new Utf8JsonStreamReader(16).ToAsyncEnumerable(Utf8(Records)))
            async.Add((r.TokenType, r.Depth));
        CollectionAssert.AreEqual(sync, async);
    }

    [TestMethod]
    public void DepthDefaultsToZeroForManuallyCreatedResults()
    {
        Assert.AreEqual(0, new JsonResult(JsonTokenType.String, "x").Depth);
        var (type, value) = new JsonResult(JsonTokenType.String, "x");
        Assert.AreEqual(JsonTokenType.String, type);
        Assert.AreEqual("x", value);
    }

    // ---- Raw subtrees ----

    static List<JsonResult> ReadRaw(
        string json,
        int rawFromDepth,
        int bufferSize = 32 * 1024,
        int maxBufferSize = 1024 * 1024 * 1024
    ) => [.. new Utf8JsonStreamReader(bufferSize, maxBufferSize).ToEnumerable(Utf8(json), rawFromDepth)];

    [TestMethod]
    public void RawSubtreesAreReturnedAsSingleResults()
    {
        var results = ReadRaw(Records, 2);

        var raws = results.Where(r => r.IsRaw).ToList();
        Assert.HasCount(
            4,
            raws.Where(r => r.Value is string s && s.Length > 0 && (s[0] == '{' || s[0] == '[')).ToList()
        );
        Assert.AreEqual(Records_Raw(0, "geo"), raws[0].Value);
        Assert.AreEqual(Records_Raw(0, "tags"), raws[1].Value);
        Assert.AreEqual(Records_Raw(1, "geo"), raws[2].Value);
        Assert.AreEqual(Records_Raw(1, "tags"), raws[3].Value);
        Assert.IsTrue(raws.All(r => r.Depth == 2));

        // Nothing from inside the raw values is produced
        Assert.IsFalse(
            results.Any(r => r.TokenType == JsonTokenType.PropertyName && r.Value is "type" or "coordinates" or "y")
        );
        // Everything else is as before: root array, two records
        Assert.AreEqual(1, results.Count(r => r.TokenType == JsonTokenType.StartArray && !r.IsRaw));
        Assert.AreEqual(2, results.Count(r => r.TokenType == JsonTokenType.StartObject && r.Depth == 1));
        Assert.AreEqual(2, results.Count(r => r.TokenType == JsonTokenType.EndObject && r.Depth == 1));
        Assert.AreEqual(0, results.Count(r => r.TokenType == JsonTokenType.EndObject && r.Depth >= 2));
    }

    [TestMethod]
    public void RawFromDepthBeyondDocumentDepthEqualsPlainRead()
    {
        var plain = new Utf8JsonStreamReader().ToEnumerable(Utf8(Records)).ToList();
        var raw = ReadRaw(Records, 100);
        CollectionAssert.AreEqual(plain, raw);
        Assert.IsFalse(raw.Any(r => r.IsRaw));
    }

    [TestMethod]
    public void RawFromDepthZeroReturnsWholeDocument()
    {
        var results = ReadRaw(Records, 0);
        Assert.HasCount(1, results);
        Assert.IsTrue(results[0].IsRaw);
        Assert.AreEqual(Records, results[0].Value);
    }

    [TestMethod]
    public void RawValuePreservesOriginalText()
    {
        const string json = """[{"a" :  { "b" : [ 1.0 ,  1e3, "\u00e6\n" ] }, "c" : 0.10}]""";
        var raws = ReadRaw(json, 2).Where(r => r.IsRaw).ToList();
        Assert.HasCount(1, raws);
        Assert.AreEqual("""{ "b" : [ 1.0 ,  1e3, "\u00e6\n" ] }""", raws[0].Value);
    }

    [TestMethod]
    public void RawValuesAreIdenticalForEveryBufferSize()
    {
        // Many records so that nested values straddle buffer boundaries, with a buffer that has to grow
        var sb = new StringBuilder("[");
        for (int i = 0; i < 300; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(
                $$$"""{"id":{{{i}}},"geo":{"type":"Point","coordinates":[{{{i}}}.5,{{{i}}}],"deep":{"a":[{"b":[{{{i}}}]}]}},"name":"n{{{i}}}","list":[{{{string.Join(",", Enumerable.Range(0, i % 17))}}}]}"""
            );
        }
        sb.Append(']');
        var json = sb.ToString();

        var expected = ReadRaw(json, 2, 1024 * 1024);
        Assert.IsTrue(expected.Count(r => r.IsRaw) == 600);

        foreach (var bufferSize in new[] { 1, 7, 16, 64, 100, 1000 })
        {
            var actual = ReadRaw(json, 2, bufferSize);
            CollectionAssert.AreEqual(expected, actual, $"bufferSize {bufferSize}");
        }
    }

    [TestMethod]
    public async Task RawAsyncMatchesSyncForEveryBufferSize()
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < 100; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append($$$"""{"id":{{{i}}},"o":{"x":[{{{i}}},{"y":"z"}]}}""");
        }
        sb.Append(']');
        var json = sb.ToString();
        var expected = ReadRaw(json, 2);
        foreach (var bufferSize in new[] { 3, 16, 50, 4096 })
        {
            var actual = new List<JsonResult>();
            await foreach (var r in new Utf8JsonStreamReader(bufferSize).ToAsyncEnumerable(Utf8(json), 2))
                actual.Add(r);
            CollectionAssert.AreEqual(expected, actual, $"bufferSize {bufferSize}");
        }
    }

    [TestMethod]
    public void RawValueLargerThanInitialBufferGrowsBuffer()
    {
        var big = new string('x', 100_000);
        var json = $$"""[{"a":{"big":"{{big}}"},"b":1}]""";
        var raws = ReadRaw(json, 2, 64).Where(r => r.IsRaw).ToList();
        Assert.HasCount(1, raws);
        Assert.AreEqual($$"""{"big":"{{big}}"}""", raws[0].Value);
    }

    [TestMethod]
    public void RawValueLargerThanMaxBufferSizeThrows()
    {
        var big = new string('x', 100_000);
        var json = $$"""[{"a":{"big":"{{big}}"},"b":1}]""";
        Assert.ThrowsExactly<JsonException>(() => ReadRaw(json, 2, 64, 1024));
    }

    [TestMethod]
    public void TruncatedRawValueThrows()
    {
        Assert.Throws<JsonException>(() => ReadRaw("""[{"a":{"b":[1,2""", 2));
        Assert.Throws<JsonException>(() => ReadRaw("""[{"a":{"b":[1,2""", 2, 8));
    }

    [TestMethod]
    public void NegativeRawFromDepthThrows()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new Utf8JsonStreamReader().ToEnumerable(Utf8("[]"), -1).ToList()
        );
    }

    [TestMethod]
    public void RawDoesNotAffectNonRawOverloadsAndInstanceCanBeReused()
    {
        var reader = new Utf8JsonStreamReader(16);
        var first = reader.ToEnumerable(Utf8(Records), 2).ToList();
        var plain = reader.ToEnumerable(Utf8(Records)).ToList();
        var second = reader.ToEnumerable(Utf8(Records), 2).ToList();
        CollectionAssert.AreEqual(first, second);
        Assert.IsFalse(plain.Any(r => r.IsRaw));
        Assert.IsGreaterThan(first.Count, plain.Count);
    }

    // ---- Property name cache ----

    [TestMethod]
    public void RepeatedPropertyNamesShareTheSameStringInstance()
    {
        var names = new Utf8JsonStreamReader(16)
            .ToEnumerable(Utf8("""[{"alpha":1,"beta":2},{"alpha":3,"beta":4},{"alpha":5}]"""))
            .Where(r => r.TokenType == JsonTokenType.PropertyName)
            .Select(r => (string)r.Value!)
            .ToList();

        CollectionAssert.AreEqual(new[] { "alpha", "beta", "alpha", "beta", "alpha" }, names);
        Assert.AreSame(names[0], names[2]);
        Assert.AreSame(names[0], names[4]);
        Assert.AreSame(names[1], names[3]);
    }

    [TestMethod]
    public void PropertyNameCacheCanBeDisabled()
    {
        var reader = new Utf8JsonStreamReader { InternPropertyNames = false };
        var names = reader
            .ToEnumerable(Utf8("""[{"alpha":1},{"alpha":2}]"""))
            .Where(r => r.TokenType == JsonTokenType.PropertyName)
            .Select(r => (string)r.Value!)
            .ToList();
        CollectionAssert.AreEqual(new[] { "alpha", "alpha" }, names);
        Assert.AreNotSame(names[0], names[1]);
    }

    [TestMethod]
    public void EscapedAndLongPropertyNamesAreCorrect()
    {
        var longName = new string('n', 1000);
        var json = $$"""[{"a\u00e6\"b":1,"{{longName}}":2},{"a\u00e6\"b":3,"{{longName}}":4}]""";
        var names = new Utf8JsonStreamReader()
            .ToEnumerable(Utf8(json))
            .Where(r => r.TokenType == JsonTokenType.PropertyName)
            .Select(r => (string)r.Value!)
            .ToList();
        CollectionAssert.AreEqual(new[] { "a\u00e6\"b", longName, "a\u00e6\"b", longName }, names);
    }

    [TestMethod]
    public void PropertyNameCacheIsBoundedAndStillCorrect()
    {
        var sb = new StringBuilder("[");
        for (int round = 0; round < 2; round++)
        {
            if (round > 0)
                sb.Append(',');
            sb.Append('{');
            for (int i = 0; i < 3000; i++)
                sb.Append(i > 0 ? "," : "").Append($"\"name_{i}\":{i}");
            sb.Append('}');
        }
        sb.Append(']');
        var names = new Utf8JsonStreamReader()
            .ToEnumerable(Utf8(sb.ToString()))
            .Where(r => r.TokenType == JsonTokenType.PropertyName)
            .Select(r => (string)r.Value!)
            .ToList();
        Assert.HasCount(6000, names);
        for (int i = 0; i < 6000; i++)
            Assert.AreEqual($"name_{i % 3000}", names[i]);
    }
}
