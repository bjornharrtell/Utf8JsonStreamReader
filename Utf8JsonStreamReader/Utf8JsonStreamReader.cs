using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Wololo.Text.Json;

public sealed partial class Utf8JsonStreamReader
{
    bool done = false;
    byte[] buffer;
    int bufferSize;
    readonly int maxBufferSize;
    int bufferLength = 0;
    int offset = 0;
    int remaining = 0;
    int readLength = 0;
    JsonReaderState jsonReaderState = new();

    public delegate void OnRead(ref Utf8JsonReader reader);

    public Utf8JsonStreamReader(int bufferSize = 1024 * 32, int maxBufferSize = 1024 * 1024 * 1024)
    {
        this.bufferSize = bufferSize;
        this.maxBufferSize = maxBufferSize;
        buffer = new byte[this.bufferSize];
    }

    int CopyRemaining()
    {
        remaining = bufferLength - offset;
        if (remaining > 0)
            Array.Copy(buffer, offset, buffer, 0, remaining);
        return bufferSize - remaining;
    }

    void GrowBuffer()
    {
        int newBufferSize = bufferSize * 2;
        byte[] newBuffer = new byte[newBufferSize];
        
        if (bufferLength > 0)
            Array.Copy(buffer, 0, newBuffer, 0, bufferLength);
        
        buffer = newBuffer;
        bufferSize = newBufferSize;
    }

    public void Read(Stream stream, OnRead onRead)
    {
        ReadAsync(stream, onRead, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    void ProcessBuffer(OnRead onRead, bool actualEOF, bool willGrowIfNeeded)
    {
        bufferLength = readLength + remaining;
        offset = 0;
        
        // Only set done = true if we're at EOF AND we won't be growing the buffer
        // This prevents JsonReader from trying to parse incomplete tokens
        done = actualEOF && !willGrowIfNeeded;
        
        var reader = new Utf8JsonReader(buffer.AsSpan(0, bufferLength), done, jsonReaderState);
        while (reader.Read())
            onRead(ref reader);
        jsonReaderState = reader.CurrentState;
        offset = (int)reader.BytesConsumed;
    }

    async ValueTask ReadStreamAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        while (true)
        {
            var length = CopyRemaining();
            if (length > 0)
                readLength = await stream.ReadAsync(new Memory<byte>(buffer, remaining, length), token).ConfigureAwait(false);
            else
                readLength = 0;
            bool actualEOF = readLength == 0;
            bool canGrow = bufferSize < maxBufferSize;
            bool willGrowIfNeeded = actualEOF && canGrow;
            ProcessBuffer(onRead, actualEOF, willGrowIfNeeded);
            if (offset > 0 || done)
                break;
            if (actualEOF)
            {
                if (canGrow)
                {
                    GrowBuffer();
                    continue;
                }
                throw new Exception($"Failure to parse JSON token buffer is too small ({bufferSize})");
            }
        }
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
        var results = new List<JsonResult>(64);
        while (!done)
        {
            ReadStreamAsync(stream, (ref Utf8JsonReader r) => AccumulateResults(ref r, results), CancellationToken.None).AsTask().GetAwaiter().GetResult();
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
}