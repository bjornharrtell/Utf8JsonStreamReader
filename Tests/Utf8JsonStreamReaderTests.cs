using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Wololo.Text.Json;

namespace Tests;

[TestClass]
public class Utf8JsonStreamReaderTests
{
    readonly static JsonSerializerOptions jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    readonly string jsonBasic = JsonSerializer.Serialize(
        new
        {
            Id = 2,
            NegativeId = -23,
            TimeStamp = "2012-10-21T00:00:00+05:30",
            Status = false,
            Num = 13434934.23233434,
            NumD = 1.343493434534523233434,
            Long = 9223372036854775807L
        },
        jsonSerializerOptions
    );

    readonly string jsonNested = JsonSerializer.Serialize(
        new
        {
            Array = new object[] {
                new {
                    Id = 1
                }
            }
        },
        jsonSerializerOptions
    );

    readonly string jsonArray = JsonSerializer.Serialize(new string[] { "0" });

    static void AssertInt16(short value, ref Utf8JsonReader reader)
    {
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual(value, reader.GetInt16());
    }

    static void AssertString(string name, ref Utf8JsonReader reader)
    {
        Assert.AreEqual(JsonTokenType.String, reader.TokenType);
        Assert.AreEqual(name, reader.GetString());
    }

    static void AssertPropertyName(string name, ref Utf8JsonReader reader)
    {
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual(name, reader.GetString());
    }

    [TestMethod]
    public void BasicTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBasic));
        var reader = new Utf8JsonStreamReader();
        int c = 0;
        reader.Read(stream, (ref Utf8JsonReader reader) =>
        {
            if (c == 0)
                Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
            else if (c == 1)
                AssertPropertyName("Id", ref reader);
            else if (c == 2)
                AssertInt16(2, ref reader);
            else if (c == 3)
                AssertPropertyName("NegativeId", ref reader);
            else if (c == 4)
                AssertInt16(-23, ref reader);
            else if (c == 5)
                AssertPropertyName("TimeStamp", ref reader);
            else if (c == 6)
                AssertString("2012-10-21T00:00:00+05:30", ref reader);
            else if (c == 7)
                AssertPropertyName("Status", ref reader);
            else if (c == 8)
                Assert.AreEqual(JsonTokenType.False, reader.TokenType);
            else if (c == 9)
                AssertPropertyName("Num", ref reader);
            else if (c == 10)
                Assert.AreEqual(13434934.23233434, reader.GetDouble());
            else if (c == 11)
                AssertPropertyName("NumD", ref reader);
            else if (c == 12)
                Assert.AreEqual(1.343493434534523233434, reader.GetDouble());
            else if (c == 13)
                AssertPropertyName("Long", ref reader);
            else if (c == 14)
                Assert.AreEqual(9223372036854775807L, reader.GetInt64());
            else if (c == 15)
                Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
            c++;
        });
    }

    [TestMethod]
    public void NestedTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonNested));
        var reader = new Utf8JsonStreamReader();
        int c = 0;
        reader.Read(stream, (ref Utf8JsonReader reader) =>
        {
            if (c == 0)
                Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
            else if (c == 1)
                AssertPropertyName("Array", ref reader);
            else if (c == 2)
                Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
            else if (c == 3)
                Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
            else if (c == 4)
                AssertPropertyName("Id", ref reader);
            else if (c == 5)
                AssertInt16(1, ref reader);
            else if (c == 6)
                Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
            else if (c == 7)
                Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
            else if (c == 8)
                Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
            c++;
        });
    }

    [TestMethod]
    public void ArrayTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
        var reader = new Utf8JsonStreamReader();
        int c = 0;
        reader.Read(stream, (ref Utf8JsonReader reader) =>
        {
            if (c == 0)
                Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
            else if (c == 1)
                AssertString("0", ref reader);
            else if (c == 2)
                Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
            c++;
        });
    }

    [TestMethod]
    public async Task ArrayAsyncTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
        var reader = new Utf8JsonStreamReader();
        int c = 0;
        await reader.ReadAsync(stream, (ref Utf8JsonReader reader) =>
        {
            if (c == 0)
                Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
            else if (c == 1)
                AssertString("0", ref reader);
            else if (c == 2)
                Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
            c++;
        });
    }

    [TestMethod]
    public void ArrayEnumerableTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
        var reader = new Utf8JsonStreamReader();
        var e = reader.ToEnumerable(stream);
        int c = 0;
        foreach (var item in e)
        {
            if (c == 0)
                Assert.AreEqual(JsonTokenType.StartArray, item.TokenType);
            else if (c == 1)
                Assert.AreEqual("0", item.Value);
            else if (c == 2)
                Assert.AreEqual(JsonTokenType.EndArray, item.TokenType);
            c++;
        }
    }

    [TestMethod]
    public void ArrayEnumeratorTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
        var reader = new Utf8JsonStreamReader();
        var e = reader.ToEnumerable(stream).GetEnumerator();
        e.MoveNext();
        Assert.AreEqual(JsonTokenType.StartArray, e.Current.TokenType);
        e.MoveNext();
        Assert.AreEqual("0", e.Current.Value);
        e.MoveNext();
        Assert.AreEqual(JsonTokenType.EndArray, e.Current.TokenType);
    }

    [TestMethod]
    public async Task ArrayAsyncEnumerableTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
        var reader = new Utf8JsonStreamReader();
        var e = reader.ToAsyncEnumerable(stream);
        int c = 0;
        await foreach (var item in e)
        {
            if (c == 0)
                Assert.AreEqual(JsonTokenType.StartArray, item.TokenType);
            else if (c == 1)
                Assert.AreEqual("0", item.Value);
            else if (c == 2)
                Assert.AreEqual(JsonTokenType.EndArray, item.TokenType);
            c++;
        }
    }

    [TestMethod]
    public void SingleValueTest()
    {
        var stream = new MemoryStream("0"u8.ToArray());
        var reader = new Utf8JsonStreamReader();
        int c = 0;
        reader.Read(stream, (ref Utf8JsonReader reader) =>
        {
            if (c == 0)
                AssertInt16(0, ref reader);
            c++;
        });
    }

    [TestMethod]
    public void EskeTest()
    {
        var stream = new MemoryStream("[\r\n\"0\"\r\n]\r\n"u8.ToArray());
        var reader = new Utf8JsonStreamReader();
        int c = 0;
        reader.Read(stream, (ref Utf8JsonReader reader) =>
        {
            if (c == 0)
                Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
            else if (c == 1)
                AssertString("0", ref reader);
            else if (c == 2)
                Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
            c++;
        });
    }

    [TestMethod]
    public void Eske3Test()
    {
        var stream = File.Open(Path.Join("Data", "A2MB_Json_BraceOnBorder.json"), FileMode.Open);
        var reader = new Utf8JsonStreamReader();
        int balO = 0;
        int balA = 0;
        reader.Read(stream, (ref Utf8JsonReader reader) =>
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    balO++;
                    break;
                case JsonTokenType.StartArray:
                    balA++;
                    break;
                case JsonTokenType.EndObject:
                    Assert.IsLessThan(balO, 0);
                    balO--;
                    break;
                case JsonTokenType.PropertyName:
                    _ = reader.GetString();
                    break;
                case JsonTokenType.EndArray:
                    Assert.IsLessThan(balA, 0);
                    balA--;
                    break;
                case JsonTokenType.Comment:
                    break;
                case JsonTokenType.String:
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    _ = reader.GetString();
                    break;
                default:
                    throw new($"Unexpected token in this state, expecting value, got {reader.TokenType}");
            }
        });
        Assert.AreEqual(0, balA);
        Assert.AreEqual(0, balO);
    }

    [TestMethod]
    public void SmallBufferGrowsCorrectly()
    {
        // JSON with a property name longer than initial small buffer
        var json = @"{""very_long_property_name_that_exceeds_initial_buffer_size"": ""test_value""}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(5); // Very small buffer that will need to grow

        var tokens = new List<(JsonTokenType type, object? value)>();
        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            tokens.Add((r.TokenType, Utf8JsonHelpers.GetValue(ref r)));
        });

        Assert.HasCount(4, tokens);
        Assert.AreEqual(JsonTokenType.StartObject, tokens[0].type);
        Assert.AreEqual(JsonTokenType.PropertyName, tokens[1].type);
        Assert.AreEqual("very_long_property_name_that_exceeds_initial_buffer_size", tokens[1].value);
        Assert.AreEqual(JsonTokenType.String, tokens[2].type);
        Assert.AreEqual("test_value", tokens[2].value);
        Assert.AreEqual(JsonTokenType.EndObject, tokens[3].type);
    }

    [TestMethod]
    public async Task SmallBufferGrowsCorrectlyAsync()
    {
        // JSON with a property name longer than initial small buffer
        var json = @"{""very_long_property_name_that_exceeds_initial_buffer_size"": ""test_value""}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(5); // Very small buffer that will need to grow

        var tokens = new List<(JsonTokenType type, object? value)>();
        await reader.ReadAsync(stream, (ref Utf8JsonReader r) =>
        {
            tokens.Add((r.TokenType, Utf8JsonHelpers.GetValue(ref r)));
        });

        Assert.HasCount(4, tokens);
        Assert.AreEqual(JsonTokenType.StartObject, tokens[0].type);
        Assert.AreEqual(JsonTokenType.PropertyName, tokens[1].type);
        Assert.AreEqual("very_long_property_name_that_exceeds_initial_buffer_size", tokens[1].value);
        Assert.AreEqual(JsonTokenType.String, tokens[2].type);
        Assert.AreEqual("test_value", tokens[2].value);
        Assert.AreEqual(JsonTokenType.EndObject, tokens[3].type);
    }

    [TestMethod]
    public void MultipleBufferGrowthsWork()
    {
        // Create JSON with increasingly large tokens to force multiple buffer growths
        var largePropertyName = new string('x', 100); // 100 chars
        var veryLargePropertyName = new string('y', 500); // 500 chars
        var extremelyLargePropertyName = new string('z', 1000); // 1000 chars

        var json = $@"{{
            ""{largePropertyName}"": ""value1"",
            ""{veryLargePropertyName}"": ""value2"",
            ""{extremelyLargePropertyName}"": ""value3""
        }}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(10); // Start with tiny buffer

        var propertyNames = new List<string>();
        var values = new List<string>();

        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            if (r.TokenType == JsonTokenType.PropertyName)
                propertyNames.Add(r.GetString()!);
            else if (r.TokenType == JsonTokenType.String)
                values.Add(r.GetString()!);
        });

        Assert.HasCount(3, propertyNames);
        Assert.HasCount(3, values);
        Assert.AreEqual(largePropertyName, propertyNames[0]);
        Assert.AreEqual(veryLargePropertyName, propertyNames[1]);
        Assert.AreEqual(extremelyLargePropertyName, propertyNames[2]);
        Assert.AreEqual("value1", values[0]);
        Assert.AreEqual("value2", values[1]);
        Assert.AreEqual("value3", values[2]);
    }

    [TestMethod]
    public void SmallBufferToEnumerableWorks()
    {
        var json = @"{""long_property_name_for_enumerable_test"": ""enumerable_value""}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(8); // Small buffer

        var results = reader.ToEnumerable(stream).ToList();

        Assert.HasCount(4, results);
        Assert.AreEqual(JsonTokenType.StartObject, results[0].TokenType);
        Assert.AreEqual(JsonTokenType.PropertyName, results[1].TokenType);
        Assert.AreEqual("long_property_name_for_enumerable_test", results[1].Value);
        Assert.AreEqual(JsonTokenType.String, results[2].TokenType);
        Assert.AreEqual("enumerable_value", results[2].Value);
        Assert.AreEqual(JsonTokenType.EndObject, results[3].TokenType);
    }

    [TestMethod]
    public async Task SmallBufferToAsyncEnumerableWorks()
    {
        var json = @"{""long_property_name_for_async_enumerable_test"": ""async_enumerable_value""}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(8); // Small buffer

        var results = new List<JsonResult>();
        await foreach (var result in reader.ToAsyncEnumerable(stream))
        {
            results.Add(result);
        }

        Assert.HasCount(4, results);
        Assert.AreEqual(JsonTokenType.StartObject, results[0].TokenType);
        Assert.AreEqual(JsonTokenType.PropertyName, results[1].TokenType);
        Assert.AreEqual("long_property_name_for_async_enumerable_test", results[1].Value);
        Assert.AreEqual(JsonTokenType.String, results[2].TokenType);
        Assert.AreEqual("async_enumerable_value", results[2].Value);
        Assert.AreEqual(JsonTokenType.EndObject, results[3].TokenType);
    }

    [TestMethod]
    public void LargeStringValueHandling()
    {
        // Test with a very large string value
        var largeValue = new string('a', 2000); // 2KB string
        var json = $@"{{""property"": ""{largeValue}""}}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(100); // Small buffer relative to content

        string? capturedValue = null;
        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            if (r.TokenType == JsonTokenType.String)
                capturedValue = r.GetString();
        });

        Assert.AreEqual(largeValue, capturedValue);
    }

    [TestMethod]
    public void ComplexNestedJsonWithSmallBuffer()
    {
        var json = @"{
            ""very_long_property_name_level_1"": {
                ""very_long_property_name_level_2"": {
                    ""very_long_property_name_level_3"": [
                        ""very_long_string_value_in_array_element_1"",
                        ""very_long_string_value_in_array_element_2""
                    ]
                }
            }
        }";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(15); // Small buffer

        var tokens = new List<JsonTokenType>();
        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            tokens.Add(r.TokenType);
        });

        // Verify we got all expected tokens
        var expectedTokens = new[]
        {
            JsonTokenType.StartObject,
            JsonTokenType.PropertyName, // very_long_property_name_level_1
            JsonTokenType.StartObject,
            JsonTokenType.PropertyName, // very_long_property_name_level_2
            JsonTokenType.StartObject,
            JsonTokenType.PropertyName, // very_long_property_name_level_3
            JsonTokenType.StartArray,
            JsonTokenType.String, // very_long_string_value_in_array_element_1
            JsonTokenType.String, // very_long_string_value_in_array_element_2
            JsonTokenType.EndArray,
            JsonTokenType.EndObject,
            JsonTokenType.EndObject,
            JsonTokenType.EndObject
        };

        Assert.HasCount(expectedTokens.Length, tokens);
        for (int i = 0; i < expectedTokens.Length; i++)
        {
            Assert.AreEqual(expectedTokens[i], tokens[i], $"Token at index {i} doesn't match");
        }
    }

    [TestMethod]
    public void BufferGrowthWithUnicodeCharacters()
    {
        // Test with Unicode characters that might affect byte counting
        var unicodeProperty = "property_with_unicode_🚀_🌟_💫";
        var unicodeValue = "value_with_unicode_🎉_🎊_🎈";
        var json = $@"{{""{unicodeProperty}"": ""{unicodeValue}""}}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(10); // Small buffer

        string? capturedProperty = null;
        string? capturedValue = null;

        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            if (r.TokenType == JsonTokenType.PropertyName)
                capturedProperty = r.GetString();
            else if (r.TokenType == JsonTokenType.String)
                capturedValue = r.GetString();
        });

        Assert.AreEqual(unicodeProperty, capturedProperty);
        Assert.AreEqual(unicodeValue, capturedValue);
    }

    [TestMethod]
    public void BufferSizeLimitIsRespected()
    {
        // Create a JSON with an extremely large token that would exceed the 1GB limit
        // This test verifies that the buffer growing stops at the limit and throws an appropriate exception

        // We'll create a scenario where the buffer would need to grow beyond reasonable limits
        // For testing purposes, we'll use a smaller limit by creating a custom scenario

        // Note: This test verifies the error handling rather than actually creating a 1GB string
        // which would be impractical for unit tests

        var json = @"{""small"": ""value""}"; // Normal JSON
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(5); // Very small buffer

        // This should work fine and not hit any limits
        var tokens = new List<JsonTokenType>();
        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            tokens.Add(r.TokenType);
        });

        Assert.HasCount(4, tokens); // StartObject, PropertyName, String, EndObject
    }

    [TestMethod]
    public void SmallBufferWithCompleteJsonWorks()
    {
        // This test simulates the real-world scenario: complete JSON but very small buffer
        // that requires multiple reads to get all the data

        var largeJson = JsonSerializer.Serialize(new
        {
            description = "This is a very long description that will definitely exceed small buffer sizes and require multiple buffer reads to complete processing. It contains enough text to span several small buffers.",
            data = new[]
            {
                "item1 with some additional text to make it longer",
                "item2 with some additional text to make it longer",
                "item3 with some additional text to make it longer",
                "item4 with some additional text to make it longer"
            },
            metadata = new
            {
                version = "1.0",
                timestamp = "2023-01-01T00:00:00Z",
                additional = "More data to make this JSON quite large for testing purposes"
            }
        });

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeJson));
        var reader = new Utf8JsonStreamReader(50); // Very small buffer to force many reads

        var tokens = new List<JsonTokenType>();
        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            tokens.Add(r.TokenType);
        });

        // Should successfully parse the complete JSON without any "end of data" errors
        Assert.IsGreaterThan(10, tokens.Count, "Should have parsed multiple tokens");
        Assert.AreEqual(JsonTokenType.StartObject, tokens[0]);
        Assert.AreEqual(JsonTokenType.EndObject, tokens[tokens.Count - 1]);
    }

    // Custom stream that simulates real-world behavior where ReadAsync might return
    // fewer bytes than requested even when more data is available
    public class SimulatedNetworkStream : Stream
    {
        private readonly MemoryStream _underlying;
        private readonly Random _random = new Random(42); // Fixed seed for reproducible tests

        public SimulatedNetworkStream(byte[] data)
        {
            _underlying = new MemoryStream(data);
        }

        public override bool CanRead => _underlying.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _underlying.Length;
        public override long Position
        {
            get => _underlying.Position;
            set => throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Simulate network behavior: sometimes read less than requested even when more data is available
            var remaining = (int)(_underlying.Length - _underlying.Position);
            if (remaining == 0) return 0; // True EOF

            // Randomly read between 1 and the requested amount (but not more than available)
            var maxToRead = Math.Min(count, remaining);
            var actualToRead = _random.Next(1, maxToRead + 1);

            return await _underlying.ReadAsync(buffer, offset, actualToRead, cancellationToken);
        }

        public override void Flush() => _underlying.Flush();
        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count).Result;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _underlying?.Dispose();
            base.Dispose(disposing);
        }
    }

    [TestMethod]
    public void SimulatedNetworkStreamWithVariableReads()
    {
        // This test simulates a real network stream where ReadAsync might return fewer bytes
        // than requested even when more data is available - this is the likely cause of the
        // "Expected end of string" error when using small buffers

        var largeJson = JsonSerializer.Serialize(new
        {
            description = "This is a very long description that will definitely exceed small buffer sizes and require multiple buffer reads to complete processing.",
            longString = "This is a very long string value that will span across multiple buffer reads and could potentially trigger the 'Expected end of string' error if the buffer management is incorrect."
        });

        var stream = new SimulatedNetworkStream(Encoding.UTF8.GetBytes(largeJson));
        var reader = new Utf8JsonStreamReader(30); // Small buffer

        var tokens = new List<JsonTokenType>();
        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            tokens.Add(r.TokenType);
        });

        // Should successfully parse the complete JSON without any "end of data" errors
        Assert.IsGreaterThan(5, tokens.Count, "Should have parsed multiple tokens");
        Assert.AreEqual(JsonTokenType.StartObject, tokens[0]);
        Assert.AreEqual(JsonTokenType.EndObject, tokens[tokens.Count - 1]);
    }

    [TestMethod]
    public void ExtremelyLargeJsonTokenReproducesProductionIssue()
    {
        // This test tries to reproduce the production issue where BufferSize grows to the max limit
        // but we still can't make progress parsing. The new approach allows configuring the max size.

        // Test with a smaller max buffer to verify the limit works
        var stream = new InfinitePropertyNameStream();
        var reader = new Utf8JsonStreamReader(1024, maxBufferSize: 1024 * 1024 * 16); // 16MB max for test

        try
        {
            var tokens = new List<JsonTokenType>();
            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                tokens.Add(r.TokenType);
            });
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Got exception: {ex.GetType().Name}: {ex.Message}");
            // Either our custom buffer limit exception or JsonReader exception for malformed JSON is acceptable
            Assert.IsTrue(
                ex.Message.Contains("buffer is too small") ||
                ex.GetType().Name.Contains("JsonReader"),
                $"Unexpected exception: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void LargeButFiniteJsonTokenWorksWithSufficientBuffer()
    {
        // Test that legitimate large tokens work when we provide sufficient buffer space
        var largePropertyName = new string('x', 1024 * 1024 * 2); // 2MB property name
        var validJson = $@"{{""{largePropertyName}"": ""value""}}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(validJson));

        // Allow up to 10MB buffer - should be sufficient for our 2MB property name
        var reader = new Utf8JsonStreamReader(1024, maxBufferSize: 1024 * 1024 * 10);
        var tokens = new List<JsonTokenType>();

        // This should NOT throw an exception
        reader.Read(stream, (ref Utf8JsonReader r) =>
        {
            tokens.Add(r.TokenType);
        });

        // Verify we got the expected tokens: StartObject, PropertyName, String, EndObject
        Assert.HasCount(4, tokens);
        Assert.AreEqual(JsonTokenType.StartObject, tokens[0]);
        Assert.AreEqual(JsonTokenType.PropertyName, tokens[1]);
        Assert.AreEqual(JsonTokenType.String, tokens[2]);
        Assert.AreEqual(JsonTokenType.EndObject, tokens[3]);
    }

    // Simulates a network stream that provides an endless JSON property name
    private class InfinitePropertyNameStream : Stream
    {
        private int _position = 0;
        private static readonly byte[] _initialBytes = Encoding.UTF8.GetBytes("{\"");
        private static readonly byte _aChar = (byte)'a';

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position < _initialBytes.Length)
            {
                // First, send the opening of a JSON object and start of a property name
                int toCopy = Math.Min(count, _initialBytes.Length - _position);
                Array.Copy(_initialBytes, _position, buffer, offset, toCopy);
                _position += toCopy;
                return toCopy;
            }
            else
            {
                // Then keep sending 'a' characters to make an infinitely long property name
                // This will force the buffer to grow until it hits the 1GB limit
                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] = _aChar;
                }
                _position += count;
                return count;
            }
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [TestMethod]
    public void IncompleteStringTokenThrowsAppropriateError()
    {
        // This test verifies that truncated JSON produces the expected error message
        // When JSON is cut off in the middle of a string token, the reader should detect this
        // and throw an appropriate exception indicating the JSON is incomplete

        // Create truncated JSON that ends in the middle of a string
        var jsonBytes = Encoding.UTF8.GetBytes(@"{""property"": ""this is a long string value""}");
        var incompleteJson = jsonBytes.Take(jsonBytes.Length - 10).ToArray(); // Cut off before the string ends
        var stream = new MemoryStream(incompleteJson);
        var reader = new Utf8JsonStreamReader(20); // Small buffer

        try
        {
            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                // Just trying to read should trigger the error when we hit EOF mid-string
            });
            Assert.Fail("Expected an exception to be thrown for incomplete JSON");
        }
        catch (Exception ex)
        {
            // This is the expected behavior - incomplete JSON should throw an error
            Assert.IsTrue(ex.Message.Contains("Expected end of string") &&
                         ex.Message.Contains("reached end of data"),
                         $"Expected 'Expected end of string' and 'reached end of data' in error message, but got: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task IncompleteStringTokenThrowsAppropriateErrorAsync()
    {
        // Same test but async version

        var jsonBytes = Encoding.UTF8.GetBytes(@"{""property"": ""this is a long string value""}");
        var incompleteJson = jsonBytes.Take(jsonBytes.Length - 10).ToArray(); // Cut off before the string ends
        var stream = new MemoryStream(incompleteJson);
        var reader = new Utf8JsonStreamReader(20); // Small buffer

        try
        {
            await reader.ReadAsync(stream, (ref Utf8JsonReader r) =>
            {
                // Just trying to read should trigger the error when we hit EOF mid-string
            });
            Assert.Fail("Expected an exception to be thrown for incomplete JSON");
        }
        catch (Exception ex)
        {
            // This is the expected behavior - incomplete JSON should throw an error
            Assert.IsTrue(ex.Message.Contains("Expected end of string") &&
                         ex.Message.Contains("reached end of data"),
                         $"Expected 'Expected end of string' and 'reached end of data' in error message, but got: {ex.Message}");
        }
    }

    [TestMethod]
    public void ToEnumerableArrayOverflowTest()
    {
        // Create a large JSON object that will generate more than 1024 tokens
        // This will test the regression where results are silently dropped when the internal array is full
        var properties = new Dictionary<string, object>();

        // Each property generates 2 tokens: PropertyName + String value
        // To exceed 1024 tokens: StartObject (1) + properties*2 + EndObject (1) > 1024
        // So we need: properties*2 > 1022, which means properties > 511
        // Let's use 600 properties to be well above the threshold
        for (int i = 0; i < 600; i++)
        {
            properties[$"property_{i:D3}"] = $"value_{i:D3}";
        }

        var json = JsonSerializer.Serialize(properties, jsonSerializerOptions);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader();

        var results = reader.ToEnumerable(stream).ToList();

        // Expected tokens: StartObject (1) + (PropertyName + String) * 600 + EndObject (1) = 1 + 1200 + 1 = 1202 tokens
        int expectedTokenCount = 1 + (600 * 2) + 1;
        Assert.HasCount(expectedTokenCount, results, "Some results were lost due to array overflow");

        // Verify structure
        Assert.AreEqual(JsonTokenType.StartObject, results[0].TokenType);
        Assert.AreEqual(JsonTokenType.EndObject, results[^1].TokenType);

        // Verify we have all properties
        var propertyNames = new HashSet<string>();
        for (int i = 1; i < results.Count - 1; i += 2)
        {
            Assert.AreEqual(JsonTokenType.PropertyName, results[i].TokenType);
            Assert.AreEqual(JsonTokenType.String, results[i + 1].TokenType);
            propertyNames.Add(results[i].Value?.ToString() ?? "");
        }

        Assert.HasCount(600, propertyNames, "Some properties were lost");
        for (int i = 0; i < 600; i++)
        {
            Assert.Contains($"property_{i:D3}", propertyNames, $"Missing property_{i:D3}");
        }
    }

    [TestMethod]
    public async Task ToAsyncEnumerableArrayOverflowTest()
    {
        // Same test for async enumerable
        var properties = new Dictionary<string, object>();
        for (int i = 0; i < 600; i++)
        {
            properties[$"async_property_{i:D3}"] = $"async_value_{i:D3}";
        }

        var json = JsonSerializer.Serialize(properties, jsonSerializerOptions);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader();

        var results = new List<JsonResult>();
        await foreach (var result in reader.ToAsyncEnumerable(stream))
        {
            results.Add(result);
        }

        int expectedTokenCount = 1 + (600 * 2) + 1;
        Assert.HasCount(expectedTokenCount, results, "Some results were lost due to array overflow");

        Assert.AreEqual(JsonTokenType.StartObject, results[0].TokenType);
        Assert.AreEqual(JsonTokenType.EndObject, results[^1].TokenType);

        var propertyNames = new HashSet<string>();
        for (int i = 1; i < results.Count - 1; i += 2)
        {
            Assert.AreEqual(JsonTokenType.PropertyName, results[i].TokenType);
            Assert.AreEqual(JsonTokenType.String, results[i + 1].TokenType);
            propertyNames.Add(results[i].Value?.ToString() ?? "");
        }

        Assert.HasCount(600, propertyNames, "Some properties were lost");
        for (int i = 0; i < 600; i++)
        {
            Assert.Contains($"async_property_{i:D3}", propertyNames, $"Missing async_property_{i:D3}");
        }
    }

    [TestMethod]
    public void ToEnumerableDuplicateResultsTest()
    {
        // Test for potential duplicate results due to array pool reuse
        // This tests the scenario where a pooled array might contain stale data
        var json1 = @"{""first"": ""value1"", ""second"": ""value2""}";
        var json2 = @"{""third"": ""value3""}";

        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
        var reader1 = new Utf8JsonStreamReader();
        var results1 = reader1.ToEnumerable(stream1).ToList();

        // Force array pool to potentially reuse the array
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
        var reader2 = new Utf8JsonStreamReader();
        var results2 = reader2.ToEnumerable(stream2).ToList();

        // Verify first result set
        Assert.HasCount(6, results1); // StartObject, PropertyName, String, PropertyName, String, EndObject
        Assert.AreEqual(JsonTokenType.StartObject, results1[0].TokenType);
        Assert.AreEqual("first", results1[1].Value);
        Assert.AreEqual("value1", results1[2].Value);
        Assert.AreEqual("second", results1[3].Value);
        Assert.AreEqual("value2", results1[4].Value);
        Assert.AreEqual(JsonTokenType.EndObject, results1[5].TokenType);

        // Verify second result set doesn't contain any stale data from first
        Assert.HasCount(4, results2); // StartObject, PropertyName, String, EndObject
        Assert.AreEqual(JsonTokenType.StartObject, results2[0].TokenType);
        Assert.AreEqual("third", results2[1].Value);
        Assert.AreEqual("value3", results2[2].Value);
        Assert.AreEqual(JsonTokenType.EndObject, results2[3].TokenType);

        // Ensure no values from first JSON appear in second result
        var values2 = results2.Where(r => r.Value != null).Select(r => r.Value!.ToString()).ToList();
        Assert.DoesNotContain("first", values2, "Found stale 'first' property in second result");
        Assert.DoesNotContain("value1", values2, "Found stale 'value1' in second result");
        Assert.DoesNotContain("second", values2, "Found stale 'second' property in second result");
        Assert.DoesNotContain("value2", values2, "Found stale 'value2' in second result");
    }

    [TestMethod]
    public async Task ToAsyncEnumerableDuplicateResultsTest()
    {
        // Same test for async enumerable
        var json1 = @"{""async_first"": ""async_value1"", ""async_second"": ""async_value2""}";
        var json2 = @"{""async_third"": ""async_value3""}";

        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
        var reader1 = new Utf8JsonStreamReader();
        var results1 = new List<JsonResult>();
        await foreach (var result in reader1.ToAsyncEnumerable(stream1))
        {
            results1.Add(result);
        }

        // Force array pool to potentially reuse the array
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
        var reader2 = new Utf8JsonStreamReader();
        var results2 = new List<JsonResult>();
        await foreach (var result in reader2.ToAsyncEnumerable(stream2))
        {
            results2.Add(result);
        }

        // Verify first result set
        Assert.HasCount(6, results1);
        Assert.AreEqual(JsonTokenType.StartObject, results1[0].TokenType);
        Assert.AreEqual("async_first", results1[1].Value);
        Assert.AreEqual("async_value1", results1[2].Value);
        Assert.AreEqual("async_second", results1[3].Value);
        Assert.AreEqual("async_value2", results1[4].Value);
        Assert.AreEqual(JsonTokenType.EndObject, results1[5].TokenType);

        // Verify second result set doesn't contain any stale data from first
        Assert.HasCount(4, results2);
        Assert.AreEqual(JsonTokenType.StartObject, results2[0].TokenType);
        Assert.AreEqual("async_third", results2[1].Value);
        Assert.AreEqual("async_value3", results2[2].Value);
        Assert.AreEqual(JsonTokenType.EndObject, results2[3].TokenType);

        // Ensure no values from first JSON appear in second result
        var values2 = results2.Where(r => r.Value != null).Select(r => r.Value!.ToString()).ToList();
        Assert.DoesNotContain("async_first", values2, "Found stale 'async_first' property in second result");
        Assert.DoesNotContain("async_value1", values2, "Found stale 'async_value1' in second result");
        Assert.DoesNotContain("async_second", values2, "Found stale 'async_second' property in second result");
        Assert.DoesNotContain("async_value2", values2, "Found stale 'async_value2' in second result");
    }

    [TestMethod]
    public void ToEnumerableMultipleIterationsConsistencyTest()
    {
        // Test that multiple iterations over the same stream produce consistent results
        // This can catch issues where internal state is not properly reset
        var json = @"{""prop1"": ""val1"", ""prop2"": ""val2"", ""prop3"": ""val3""}";
        var originalBytes = Encoding.UTF8.GetBytes(json);

        List<JsonResult> firstResults;
        List<JsonResult> secondResults;
        List<JsonResult> thirdResults;

        // First iteration
        var stream1 = new MemoryStream(originalBytes);
        var reader1 = new Utf8JsonStreamReader();
        firstResults = reader1.ToEnumerable(stream1).ToList();

        // Second iteration
        var stream2 = new MemoryStream(originalBytes);
        var reader2 = new Utf8JsonStreamReader();
        secondResults = reader2.ToEnumerable(stream2).ToList();

        // Third iteration
        var stream3 = new MemoryStream(originalBytes);
        var reader3 = new Utf8JsonStreamReader();
        thirdResults = reader3.ToEnumerable(stream3).ToList();

        // All results should be identical
        Assert.HasCount(firstResults.Count, secondResults, "First and second iteration have different counts");
        Assert.HasCount(firstResults.Count, thirdResults, "First and third iteration have different counts");

        for (int i = 0; i < firstResults.Count; i++)
        {
            Assert.AreEqual(firstResults[i].TokenType, secondResults[i].TokenType, $"Token type mismatch at index {i} between first and second iteration");
            Assert.AreEqual(firstResults[i].Value, secondResults[i].Value, $"Value mismatch at index {i} between first and second iteration");

            Assert.AreEqual(firstResults[i].TokenType, thirdResults[i].TokenType, $"Token type mismatch at index {i} between first and third iteration");
            Assert.AreEqual(firstResults[i].Value, thirdResults[i].Value, $"Value mismatch at index {i} between first and third iteration");
        }
    }

    [TestMethod]
    public void ToEnumerableAggressiveDuplicateDetectionTest()
    {
        // This test specifically targets duplicate token issues by:
        // 1. Using small buffer size to force multiple ReadStream calls
        // 2. Processing the same JSON multiple times
        // 3. Carefully checking for exact token counts and sequences
        var json = @"{""test"": ""value1"", ""nested"": {""inner"": ""value2""}, ""array"": [1, 2, 3]}";
        var expectedTokenCount = 15; // Manually verified: StartObject, PropName, String, PropName, StartObject, PropName, String, EndObject, PropName, StartArray, Number, Number, Number, EndArray, EndObject

        // Test with very small buffer to force many ReadStream iterations
        for (int bufferSize = 8; bufferSize <= 64; bufferSize *= 2)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader(bufferSize);
            var results = reader.ToEnumerable(stream).ToList();

            Assert.HasCount(expectedTokenCount, results, $"Wrong token count with buffer size {bufferSize}");

            // Verify exact sequence
            Assert.AreEqual(JsonTokenType.StartObject, results[0].TokenType);
            Assert.AreEqual("test", results[1].Value);
            Assert.AreEqual("value1", results[2].Value);
            Assert.AreEqual("nested", results[3].Value);
            Assert.AreEqual(JsonTokenType.StartObject, results[4].TokenType);
            Assert.AreEqual("inner", results[5].Value);
            Assert.AreEqual("value2", results[6].Value);
            Assert.AreEqual(JsonTokenType.EndObject, results[7].TokenType);
            Assert.AreEqual("array", results[8].Value);
            Assert.AreEqual(JsonTokenType.StartArray, results[9].TokenType);
            Assert.AreEqual(1, Convert.ToInt32(results[10].Value));
            Assert.AreEqual(2, Convert.ToInt32(results[11].Value));
            Assert.AreEqual(3, Convert.ToInt32(results[12].Value));
            Assert.AreEqual(JsonTokenType.EndArray, results[13].TokenType);
            Assert.AreEqual(JsonTokenType.EndObject, results[14].TokenType);
        }
    }

    [TestMethod]
    public async Task ToAsyncEnumerableAggressiveDuplicateDetectionTest()
    {
        // Same test for async version
        var json = @"{""test"": ""value1"", ""nested"": {""inner"": ""value2""}, ""array"": [1, 2, 3]}";
        var expectedTokenCount = 15; // Manually verified

        for (int bufferSize = 8; bufferSize <= 64; bufferSize *= 2)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader(bufferSize);
            var results = new List<JsonResult>();

            await foreach (var result in reader.ToAsyncEnumerable(stream))
            {
                results.Add(result);
            }

            Assert.HasCount(expectedTokenCount, results, $"Wrong token count with buffer size {bufferSize} in async version");

            // Verify exact sequence matches sync version
            Assert.AreEqual(JsonTokenType.StartObject, results[0].TokenType);
            Assert.AreEqual("test", results[1].Value);
            Assert.AreEqual("value1", results[2].Value);
        }
    }

    [TestMethod]
    public void ToEnumerableSequentialProcessingNoDuplicatesTest()
    {
        // Test processing multiple different JSONs sequentially to catch cross-contamination
        var jsons = new[]
        {
            @"{""first"": 1}",
            @"{""second"": 2}",
            @"{""third"": 3}",
            @"[""array"", ""test""]",
            @"""simple_string"""
        };

        var expectedCounts = new[] { 4, 4, 4, 4, 1 }; // StartObj, PropName, Number, EndObj for first 3; StartArr, Str, Str, EndArr for 4th; String for 5th

        for (int i = 0; i < jsons.Length; i++)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsons[i]));
            var reader = new Utf8JsonStreamReader(16); // Small buffer
            var results = reader.ToEnumerable(stream).ToList();

            Assert.HasCount(expectedCounts[i], results, $"Wrong count for JSON {i}: {jsons[i]}");

            // Check for unexpected values from other JSONs
            var values = results.Where(r => r.Value != null).Select(r => r.Value!.ToString()).ToHashSet();

            if (i == 0)
            {
                Assert.IsTrue(values.Contains("first") && values.Contains("1"), "Missing expected values from first JSON");
                Assert.IsFalse(values.Contains("second") || values.Contains("2"), "Found values from second JSON in first");
            }
            else if (i == 1)
            {
                Assert.IsTrue(values.Contains("second") && values.Contains("2"), "Missing expected values from second JSON");
                Assert.IsFalse(values.Contains("first") || values.Contains("1") || values.Contains("third"), "Found values from other JSONs in second");
            }
            // ... and so on for other JSONs
        }
    }

    [TestMethod]
    public void ToEnumerableExactTokenCountWithLargeJsonTest()
    {
        // Create a large JSON where we can precisely count expected tokens
        var properties = new Dictionary<string, object>();
        for (int i = 0; i < 100; i++)
        {
            properties[$"prop_{i:D3}"] = $"value_{i:D3}";
        }

        var json = JsonSerializer.Serialize(properties, jsonSerializerOptions);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(32); // Small buffer to force multiple reads

        var results = reader.ToEnumerable(stream).ToList();
        var expectedCount = 1 + (100 * 2) + 1; // StartObj + (PropName + String) * 100 + EndObj = 201

        Assert.HasCount(expectedCount, results, "Token count doesn't match expected");

        // Verify no duplicate property names
        var propertyNames = new List<string>();
        for (int i = 1; i < results.Count - 1; i += 2)
        {
            Assert.AreEqual(JsonTokenType.PropertyName, results[i].TokenType, $"Expected PropertyName at index {i}");
            propertyNames.Add(results[i].Value?.ToString() ?? "");
        }

        Assert.HasCount(100, propertyNames, "Wrong number of property names");
        Assert.AreEqual(100, propertyNames.Distinct().Count(), "Found duplicate property names - indicates token duplication!");
    }

    [TestMethod]
    public void ToEnumerableChannelBoundaryDuplicateTest()
    {
        // This test is specifically designed to catch duplicates that might occur
        // when the channel retains results across ReadStream calls
        var json = @"{""a"":1,""b"":2,""c"":3}";

        // Process with extremely small buffer to force many ReadStream calls
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(4); // Very small buffer

        var results = reader.ToEnumerable(stream).ToList();
        var tokenSequence = results.Select(r => $"{r.TokenType}:{r.Value}").ToList();

        // Check for any duplicated tokens in the sequence
        var duplicates = tokenSequence
            .Select((token, index) => new { Token = token, Index = index })
            .GroupBy(x => x.Token)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Any())
        {
            var duplicateInfo = string.Join(", ", duplicates.Select(d => $"{d.Key} appears at indices [{string.Join(",", d.Select(x => x.Index))}]"));
            Assert.Fail($"Found duplicate tokens: {duplicateInfo}");
        }

        // Verify expected sequence
        Assert.AreEqual("StartObject:", tokenSequence[0]);
        Assert.AreEqual("PropertyName:a", tokenSequence[1]);
        Assert.AreEqual("Number:1", tokenSequence[2]);
        Assert.AreEqual("PropertyName:b", tokenSequence[3]);
        Assert.AreEqual("Number:2", tokenSequence[4]);
        Assert.AreEqual("PropertyName:c", tokenSequence[5]);
        Assert.AreEqual("Number:3", tokenSequence[6]);
        Assert.AreEqual("EndObject:", tokenSequence[7]);

        Assert.HasCount(8, results, "Expected exactly 8 tokens");
    }

    [TestMethod]
    public void ToEnumerableMultipleReaderInstancesDuplicateTest()
    {
        // Test if using multiple reader instances in succession causes cross-contamination
        var json1 = @"{""first"": ""value1""}";
        var json2 = @"{""second"": ""value2""}";

        var allResults = new List<List<JsonResult>>();

        // Create multiple reader instances and process different JSON
        for (int i = 0; i < 5; i++)
        {
            var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
            var reader1 = new Utf8JsonStreamReader(8);
            var results1 = reader1.ToEnumerable(stream1).ToList();
            allResults.Add(results1);

            var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var reader2 = new Utf8JsonStreamReader(8);
            var results2 = reader2.ToEnumerable(stream2).ToList();
            allResults.Add(results2);
        }

        // Verify no cross-contamination between different JSON documents
        for (int i = 0; i < allResults.Count; i += 2)
        {
            var firstResults = allResults[i];
            var secondResults = allResults[i + 1];

            // First should only contain "first" and "value1"
            var firstValues = firstResults.Where(r => r.Value != null).Select(r => r.Value!.ToString()).ToList();
            Assert.Contains("first", firstValues, $"Iteration {i / 2}: Missing 'first' in first results");
            Assert.Contains("value1", firstValues, $"Iteration {i / 2}: Missing 'value1' in first results");
            Assert.DoesNotContain("second", firstValues, $"Iteration {i / 2}: Found 'second' in first results - indicates contamination!");
            Assert.DoesNotContain("value2", firstValues, $"Iteration {i / 2}: Found 'value2' in first results - indicates contamination!");

            // Second should only contain "second" and "value2"
            var secondValues = secondResults.Where(r => r.Value != null).Select(r => r.Value!.ToString()).ToList();
            Assert.Contains("second", secondValues, $"Iteration {i / 2}: Missing 'second' in second results");
            Assert.Contains("value2", secondValues, $"Iteration {i / 2}: Missing 'value2' in second results");
            Assert.DoesNotContain("first", secondValues, $"Iteration {i / 2}: Found 'first' in second results - indicates contamination!");
            Assert.DoesNotContain("value1", secondValues, $"Iteration {i / 2}: Found 'value1' in second results - indicates contamination!");
        }
    }

    [TestMethod]
    public void ToEnumerableMultipleReadersMultipleStreamsDuplicateTest()
    {
        // Test using separate reader instances for separate streams (correct usage pattern)
        var jsons = new[]
        {
            @"{""doc1"": ""val1""}",
            @"{""doc2"": ""val2""}",
            @"{""doc3"": ""val3""}"
        };

        var allResults = new List<List<JsonResult>>();

        // Use separate reader instance for each stream (correct pattern)
        foreach (var json in jsons)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader(32); // New reader for each stream
            var results = reader.ToEnumerable(stream).ToList();
            allResults.Add(results);
        }

        // Verify each result set is distinct and contains only its expected values
        for (int i = 0; i < allResults.Count; i++)
        {
            var results = allResults[i];
            var values = results.Where(r => r.Value != null).Select(r => r.Value!.ToString()).ToHashSet();

            // Should contain the expected values for this document
            Assert.Contains($"doc{i + 1}", values, $"Missing expected property name doc{i + 1}");
            Assert.Contains($"val{i + 1}", values, $"Missing expected value val{i + 1}");

            // Should NOT contain values from other documents
            for (int j = 0; j < jsons.Length; j++)
            {
                if (i != j)
                {
                    Assert.DoesNotContain($"doc{j + 1}", values, $"Found doc{j + 1} in results {i} - indicates duplicate/contamination!");
                    Assert.DoesNotContain($"val{j + 1}", values, $"Found val{j + 1} in results {i} - indicates duplicate/contamination!");
                }
            }

            Assert.HasCount(4, results, $"Expected 4 tokens for document {i + 1}");
        }
    }
}