using System.Text.Json;

namespace Wololo.Text.Json;

public sealed partial class Utf8JsonStreamReader
{
    bool done = false;
    Memory<byte> buffer;
    int bufferSize;
    int bufferLength = 0;
    int offset = 0;
    JsonReaderState jsonReaderState = new();

    public delegate void OnRead(ref Utf8JsonReader reader);

    public Utf8JsonStreamReader(int bufferSize = -1)
    {
        this.bufferSize = bufferSize <= 0 ? 1024 * 8 : bufferSize;
        buffer = new byte[this.bufferSize];
    }

    public void Read(Stream stream, OnRead onRead)
    {
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = stream.ReadAtLeast(buffer[remaining..].Span, bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            ReadBuffer(onRead);
        }
    }

    public async Task ReadAsync(Stream stream, OnRead onRead)
    {
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = await stream.ReadAtLeastAsync(buffer[remaining..], bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            ReadBuffer(onRead);
        }
    }

    public IEnumerable<JsonResult> ToEnumerable(Stream stream)
    {
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = stream.ReadAtLeast(buffer[remaining..].Span, bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            var results = new Queue<JsonResult>();
            ReadBuffer((ref Utf8JsonReader reader) => results.Enqueue(new JsonResult(reader.TokenType, Utf8JsonHelpers.GetValue(ref reader))));
            foreach (var item in results)
                yield return item;
        }
    }

    public async IAsyncEnumerable<JsonResult> ToAsyncEnumerable(Stream stream)
    {
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = await stream.ReadAtLeastAsync(buffer[remaining..], bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            var results = new Queue<JsonResult>();
            ReadBuffer((ref Utf8JsonReader reader) => results.Enqueue(new JsonResult(reader.TokenType, Utf8JsonHelpers.GetValue(ref reader))));
            foreach (var item in results)
                yield return item;
        }
    }

    private void ReadBuffer(OnRead onRead)
    {
        var reader = new Utf8JsonReader(buffer[offset..bufferLength].Span, done, jsonReaderState);
        while (reader.Read())
            onRead(ref reader);
        jsonReaderState = reader.CurrentState;
        offset = (int)reader.BytesConsumed;
    }
}