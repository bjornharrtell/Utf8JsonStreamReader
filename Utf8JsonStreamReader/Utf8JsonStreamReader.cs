using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Wololo.Text.Json;

public sealed partial class Utf8JsonStreamReader
{
    bool done = false;
    readonly Memory<byte> buffer;
    readonly int bufferSize;
    int bufferLength = 0;
    int offset = 0;
    JsonReaderState jsonReaderState = new();

    public delegate void OnRead(ref Utf8JsonReader reader);

    public Utf8JsonStreamReader(int bufferSize = -1)
    {
        this.bufferSize = bufferSize <= 0 ? 1024 * 8 : bufferSize;
        buffer = new byte[this.bufferSize];
    }

    public bool Read(Stream stream, OnRead onRead)
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
        return true;
    }

    public async Task<bool> ReadAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = await stream.ReadAtLeastAsync(buffer[remaining..], bufferSize - remaining, false, token);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            ReadBuffer(onRead);
        }
        return true;
    }

    public IEnumerable<JsonResult> ToEnumerable(Stream stream)
    {
        var results = new List<JsonResult>();
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = stream.ReadAtLeast(buffer[remaining..].Span, bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            ReadBuffer((ref Utf8JsonReader reader) =>
                results.Add(new JsonResult(reader.TokenType, Utf8JsonHelpers.GetValue(ref reader)))
            );
            foreach (var item in results)
                yield return item;
            results.Clear();
        }
    }

    public async IAsyncEnumerable<JsonResult> ToAsyncEnumerable(Stream stream, [EnumeratorCancellation] CancellationToken token = default)
    {
        var results = new List<JsonResult>();
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = await stream.ReadAtLeastAsync(buffer[remaining..], bufferSize - remaining, false, token);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            ReadBuffer((ref Utf8JsonReader reader) =>
                results.Add(new JsonResult(reader.TokenType, Utf8JsonHelpers.GetValue(ref reader)))
            );
            foreach (var item in results)
                yield return item;
            results.Clear();
            if (token.IsCancellationRequested)
                break;
        }
    }

    private void ReadBuffer(OnRead onRead)
    {
        var reader = new Utf8JsonReader(buffer[offset..bufferLength].Span, done, jsonReaderState);
        while (reader.Read())
            onRead(ref reader);
        jsonReaderState = reader.CurrentState;
        offset = (int)reader.BytesConsumed;
        if (offset == 0)
            throw new Exception("Failure to parse JSON token buffer is to small");
    }
}