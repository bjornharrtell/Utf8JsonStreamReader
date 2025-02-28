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
    int remaining = 0;
    int readLength = 0;
    JsonReaderState jsonReaderState = new();

    public delegate void OnRead(ref Utf8JsonReader reader);

    public Utf8JsonStreamReader(int bufferSize = -1)
    {
        this.bufferSize = bufferSize <= 0 ? 1024 * 32 : bufferSize;
        buffer = new byte[this.bufferSize];
    }

    void CopyRemaining()
    {
        remaining = bufferLength - offset;
        if (remaining > 0)
            buffer[offset..].CopyTo(buffer);
    }

    void ReadStream(Stream stream, OnRead onRead)
    {
        CopyRemaining();
        readLength = stream.ReadAtLeast(buffer[remaining..].Span, bufferSize - remaining, false);
        ReadBuffer(onRead);
    }

    public void Read(Stream stream, OnRead onRead)
    {
        while (!done)
            ReadStream(stream, onRead);
    }

    async ValueTask ReadStreamAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        CopyRemaining();
        readLength = await stream.ReadAtLeastAsync(buffer[remaining..], bufferSize - remaining, false, token).ConfigureAwait(false);
        ReadBuffer(onRead);
    }

    public async ValueTask ReadAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        while (!done)
            await ReadStreamAsync(stream, onRead, token).ConfigureAwait(false);
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

    public async IAsyncEnumerable<JsonResult> ToAsyncEnumerable(Stream stream, [EnumeratorCancellation] CancellationToken token = default)
    {
        var results = new List<JsonResult>();
        while (!done)
        {
            await ReadStreamAsync(stream, (ref Utf8JsonReader r) => AccumulateResults(ref r, results), token).ConfigureAwait(false);
            foreach (var item in results)
                yield return item;
            results.Clear();
            if (token.IsCancellationRequested)
                break;
        }
    }

    private void ReadBuffer(OnRead onRead)
    {
        bufferLength = readLength + remaining;
        offset = 0;
        done = bufferLength < bufferSize;
        var reader = new Utf8JsonReader(buffer[offset..bufferLength].Span, done, jsonReaderState);
        while (reader.Read())
            onRead(ref reader);
        jsonReaderState = reader.CurrentState;
        offset = (int)reader.BytesConsumed;
        if (offset == 0)
            throw new Exception("Failure to parse JSON token buffer is too small");
    }
}