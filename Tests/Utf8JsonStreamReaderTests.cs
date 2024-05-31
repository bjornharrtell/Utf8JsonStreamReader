using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                    Assert.IsTrue(0 < balO);
                    balO--;
                    break;
                case JsonTokenType.PropertyName:
                    _ = reader.GetString();
                    break;
                case JsonTokenType.EndArray:
                    Assert.IsTrue(0 < balA);
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
}