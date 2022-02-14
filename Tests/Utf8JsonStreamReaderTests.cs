using System.Text;
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
        }
    );

    [TestMethod]
    public void BasicTest()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var reader = new Utf8JsonStreamReader(stream, 10);
        reader.Read();
        Assert.AreEqual(JsonTokenType.StartObject, reader.TokenType);
        reader.Read();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Id", reader.GetString());
        reader.Read();
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        Assert.AreEqual(2, reader.GetInt32());
        reader.Read();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("TimeStamp", reader.GetString());
        reader.Read();
        Assert.AreEqual(JsonTokenType.String, reader.TokenType);
        Assert.AreEqual(DateTimeOffset.Parse("2012-10-21T00:00:00+05:30"), reader.GetDateTimeOffset());
        reader.Read();
        Assert.AreEqual(JsonTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("Status", reader.GetString());
        reader.Read();
        Assert.AreEqual(JsonTokenType.False, reader.TokenType);
        reader.Read();
        Assert.AreEqual(JsonTokenType.EndObject, reader.TokenType);
        Assert.AreEqual(false, reader.Read());
    }
}