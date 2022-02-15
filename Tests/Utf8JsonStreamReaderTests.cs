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
            Num = 13434934.23233434
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

    [TestMethod]
    public async Task BasicTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBasic));
        var reader = new Utf8JsonStreamReader(stream);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Id", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((short)2, reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("NegativeId", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((short)-23, reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("TimeStamp", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.String, reader.TokenType);
        Assert.AreEqual("2012-10-21T00:00:00+05:30", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Status", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.False, reader.TokenType);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Num", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual((double)13434934.23233434, reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
    }

    [TestMethod]
    public async Task NestedTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonNested));
        var reader = new Utf8JsonStreamReader(stream);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Array", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.StartArray, reader.TokenType);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Id", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((short)1, reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.EndArray, reader.TokenType);
        var result = await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
        Assert.AreEqual(false, result);
    }
}