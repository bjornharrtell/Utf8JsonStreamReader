using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wololo.Text.Json;

namespace Tests;

[TestClass]
public class Utf8JsonStreamReaderTests
{
    readonly string json = JsonSerializer.Serialize(
        new {
            Id = 2,
            TimeStamp = "2012-10-21T00:00:00+05:30",
            Status = false
        },
        new JsonSerializerOptions() {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        }
    );

    [TestMethod]
    public async Task Basic2Test()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(stream);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Id", reader.Value);
        await reader.ReadAsync(CancellationToken.None);
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual((byte) 2, reader.Value);
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
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
    }
}