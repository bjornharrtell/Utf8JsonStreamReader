using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Wololo.Text.Json;

public sealed partial class Utf8JsonStreamReader
{
    bool done = false;
    Memory<byte> buffer;
    int bufferSize;
    readonly int maxBufferSize;
    int bufferLength = 0;
    int offset = 0;
    int remaining = 0;
    int readLength = 0;
    JsonReaderState jsonReaderState = new();

    const int DefaultBufferSize = 1024 * 32;
    const int DefaultMaxBufferSize = 1024 * 1024 * 1024;

    public delegate void OnRead(ref Utf8JsonReader reader);

    public Utf8JsonStreamReader(int bufferSize = DefaultBufferSize, int maxBufferSize = DefaultMaxBufferSize)
    {
        this.bufferSize = bufferSize;
        this.maxBufferSize = maxBufferSize;
        buffer = new byte[this.bufferSize];
    }

    void CopyRemaining()
    {
        remaining = bufferLength - offset;
        if (remaining > 0)
            buffer[offset..].CopyTo(buffer);
    }

    bool TryGrowBuffer()
    {
        var newBufferSize = bufferSize * 2;
        if (newBufferSize > maxBufferSize)
        {
            if (bufferSize < maxBufferSize)
                newBufferSize = maxBufferSize;
            else
                return false;
        }
        var newBuffer = new byte[newBufferSize];
        buffer[..bufferLength].CopyTo(newBuffer);
        buffer = newBuffer;
        bufferSize = newBufferSize;
        return true;
    }

    void ReadStream(Stream stream, OnRead onRead)
    {
        CopyRemaining();
        readLength = stream.ReadAtLeast(buffer[remaining..].Span, bufferSize - remaining, false);
        if (!ReadBuffer(onRead))
            ReadStream(stream, onRead);
    }

    public void Read(Stream stream, OnRead onRead)
    {
        while (!done)
            ReadStream(stream, onRead);
    }

    async ValueTask ReadStreamAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        CopyRemaining();
        readLength = await stream.ReadAtLeastAsync(buffer[remaining..], bufferSize - remaining, false, token);
        if (!ReadBuffer(onRead))
            await ReadStreamAsync(stream, onRead, token);
    }

    public async ValueTask ReadAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        while (!done)
            await ReadStreamAsync(stream, onRead, token);
    }

    static void AccumulateResults(ref Utf8JsonReader reader, List<JsonResult> results) =>
        results.Add(new JsonResult(reader.TokenType, Utf8JsonHelpers.GetValue(ref reader)));

    public IEnumerable<JsonResult> ToEnumerable(Stream stream)
    {
        var results = new List<JsonResult>();
        while (!done)
        {
            ReadStream(stream, (ref Utf8JsonReader r) => AccumulateResults(ref r, results));
            foreach (var item in results)
                yield return item;
            results.Clear();
        }
    }

    public async IAsyncEnumerable<JsonResult> ToAsyncEnumerable(
        Stream stream,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        var results = new List<JsonResult>();
        while (!done)
        {
            await ReadStreamAsync(stream, (ref Utf8JsonReader r) => AccumulateResults(ref r, results), token);
            foreach (var item in results)
                yield return item;
            results.Clear();
            if (token.IsCancellationRequested)
                break;
        }
    }

    private bool ReadBuffer(OnRead onRead)
    {
        bufferLength = readLength + remaining;
        offset = 0;
        done = bufferLength < bufferSize;
        var reader = new Utf8JsonReader(buffer[offset..bufferLength].Span, done, jsonReaderState);
        while (reader.Read())
            onRead(ref reader);
        jsonReaderState = reader.CurrentState;
        offset = (int)reader.BytesConsumed;
        if (offset == 0 && !done)
        {
            if (TryGrowBuffer())
                return false;
            else
                throw new Exception("Failure to parse JSON token buffer is too small");
        }
        if (offset == 0 && done)
            throw new Exception("Failure to parse JSON token buffer is too small");
        return true;
    }
}
