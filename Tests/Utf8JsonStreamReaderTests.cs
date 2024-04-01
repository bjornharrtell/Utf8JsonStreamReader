using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wololo.Text.Json;

namespace Tests;

[TestClass]
public class Utf8JsonStreamReaderTests
{
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
        new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        }
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
        new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        }
    );

    readonly string jsonArray = JsonSerializer.Serialize(
        new string[] { "0" }
    );

    [TestMethod]
    public async Task BasicTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBasic));
        var reader = new Utf8JsonStreamReader(stream, -1, true);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Id", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((short)2, reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("NegativeId", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((short)-23, reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("TimeStamp", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.String, reader.TokenType);
        Assert.AreEqual("2012-10-21T00:00:00+05:30", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Status", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.False, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Num", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(13434934.23233434, reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("NumD", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(1.343493434534523233434, reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Long", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(9223372036854775807L, reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
        var result = await reader.ReadAsync();
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public async Task NestedTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonNested));
        var reader = new Utf8JsonStreamReader(stream, -1, true);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Array", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Id", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((short)1, reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
        var result = await reader.ReadAsync();
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public async Task ArrayTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
        var reader = new Utf8JsonStreamReader(stream, -1, true);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.String, reader.TokenType);
        Assert.AreEqual("0", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
        var result = await reader.ReadAsync();
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void ArraySyncTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
        var reader = new Utf8JsonStreamReader(stream);
        reader.Read();
        Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
        reader.Read();
        Assert.AreEqual(JsonTokenType.String, reader.TokenType);
        Assert.AreEqual("0", reader.Value);
        reader.Read();
        Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
        var result = reader.Read();
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public async Task SingleValueTest()
    {
        var stream = new MemoryStream("0"u8.ToArray());
        var reader = new Utf8JsonStreamReader(stream, -1, true);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((short)0, reader.Value);
        var result = await reader.ReadAsync();
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public async Task EskeTest()
    {
        var stream = new MemoryStream("[\r\n\"0\"\r\n]\r\n"u8.ToArray());
        var reader = new Utf8JsonStreamReader(stream, -1, true);
        Assert.AreEqual(JsonTokenType.None, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.String, reader.TokenType);
        Assert.AreEqual("0", reader.Value);
        await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
        var result = await reader.ReadAsync();
        Assert.AreEqual(JsonTokenType.None, reader.TokenType);
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public async Task Eske2Test()
    {
        var stream = new MemoryStream("[\r\n\"0\"\r\n]\r\n"u8.ToArray());
        var e = new Utf8JsonStreamTokenAsyncEnumerable(stream).GetAsyncEnumerator();
        await e.MoveNextAsync();
        Assert.AreEqual(JsonTokenType.StartArray, e.Current.TokenType);
        await e.MoveNextAsync();
        Assert.AreEqual(JsonTokenType.String, e.Current.TokenType);
        Assert.AreEqual("0", e.Current.Value);
        await e.MoveNextAsync();
        Assert.AreEqual(JsonTokenType.EndArray, e.Current.TokenType);
        var result = await e.MoveNextAsync();
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void Eske3Test()
    {
        var stream = File.Open(Path.Join("Data", "A2MB_Json_BraceOnBorder.json"), FileMode.Open);
        Utf8JsonStreamReader reader = new(stream);
        int balO = 0;
        int balA = 0;
        while (reader.Read())
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
                    _ = reader.Value;
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
                    _ = reader.Value;
                    break;
                default:
                    throw new($"Unexpected token in this state, expecting value, got {reader.TokenType}");
            }
        }
        Assert.AreEqual(0, balA);
        Assert.AreEqual(0, balO);
    }
}