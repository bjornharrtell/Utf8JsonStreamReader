using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Wololo.Text.Json;

public sealed partial class Utf8JsonStreamReader(
    int bufferSize = Utf8JsonStreamReader.DefaultBufferSize,
    int maxBufferSize = Utf8JsonStreamReader.DefaultMaxBufferSize
) : IDisposable
{
    bool done = false;
    bool disposed = false;
    byte[] buffer = ArrayPool<byte>.Shared.Rent(ValidateBufferSize(bufferSize));
    int bufferSize = bufferSize;
    readonly int maxBufferSize = maxBufferSize;
    int bufferLength = 0;
    int offset = 0;
    int remaining = 0;
    int readLength = 0;
    JsonReaderState jsonReaderState = new();

    const int DefaultBufferSize = 1024 * 32;
    const int DefaultMaxBufferSize = 1024 * 1024 * 1024;

    public delegate void OnRead(ref Utf8JsonReader reader);

    static int ValidateBufferSize(int bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        return bufferSize;
    }

    // Prepares for a new read: guards against use after dispose and resets state so an
    // instance can be reused for another stream once a previous read has completed.
    void BeginRead()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (done)
        {
            done = false;
            bufferLength = 0;
            offset = 0;
            remaining = 0;
            readLength = 0;
            jsonReaderState = new();
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
            disposed = true;
        }
    }

    void CopyRemaining()
    {
        remaining = bufferLength - offset;
        if (remaining > 0)
            buffer.AsSpan(offset, remaining).CopyTo(buffer);
    }

    bool TryGrowBuffer()
    {
        var newBufferSize = (int)Math.Min((long)bufferSize * 2, int.MaxValue);
        if (newBufferSize > maxBufferSize)
        {
            if (bufferSize < maxBufferSize)
                newBufferSize = maxBufferSize;
            else
                return false;
        }
        var newBuffer = ArrayPool<byte>.Shared.Rent(newBufferSize);
        buffer.AsSpan(0, bufferLength).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
        bufferSize = newBufferSize;
        return true;
    }

    void ReadStream(Stream stream, OnRead onRead)
    {
        do
        {
            CopyRemaining();
            readLength = stream.ReadAtLeast(
                buffer.AsSpan(remaining, bufferSize - remaining),
                bufferSize - remaining,
                false
            );
        } while (!ReadBuffer(onRead, default));
    }

    public void Read(Stream stream, OnRead onRead)
    {
        BeginRead();
        while (!done)
            ReadStream(stream, onRead);
    }

    async ValueTask ReadStreamAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        while (true)
        {
            CopyRemaining();
            readLength = await stream.ReadAtLeastAsync(
                buffer.AsMemory(remaining, bufferSize - remaining),
                bufferSize - remaining,
                false,
                token
            );
            if (ReadBuffer(onRead, token))
                break;
        }
    }

    public async ValueTask ReadAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        BeginRead();
        while (!done && !token.IsCancellationRequested)
            await ReadStreamAsync(stream, onRead, token);
    }

    static void AccumulateResults(ref Utf8JsonReader reader, List<JsonResult> results) =>
        results.Add(new JsonResult(reader.TokenType, Utf8JsonHelpers.GetValue(ref reader)));

    public IEnumerable<JsonResult> ToEnumerable(Stream stream)
    {
        BeginRead();
        var results = new List<JsonResult>();
        void onRead(ref Utf8JsonReader r) => AccumulateResults(ref r, results);
        while (!done)
        {
            ReadStream(stream, onRead);
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
        BeginRead();
        var results = new List<JsonResult>();
        void onRead(ref Utf8JsonReader r) => AccumulateResults(ref r, results);
        while (!done && !token.IsCancellationRequested)
        {
            await ReadStreamAsync(stream, onRead, token);
            foreach (var item in results)
            {
                if (token.IsCancellationRequested)
                    yield break;
                yield return item;
            }
            results.Clear();
        }
    }

    private bool ReadBuffer(OnRead onRead, CancellationToken token = default)
    {
        bufferLength = readLength + remaining;
        offset = 0;
        done = bufferLength < bufferSize;
        var reader = new Utf8JsonReader(buffer.AsSpan(0, bufferLength), done, jsonReaderState);
        while (reader.Read() && !token.IsCancellationRequested)
            onRead(ref reader);
        if (token.IsCancellationRequested)
        {
            done = true;
            return true;
        }
        jsonReaderState = reader.CurrentState;
        offset = (int)reader.BytesConsumed;
        if (offset == 0)
        {
            if (!done && TryGrowBuffer())
                return false;
            throw new JsonException("Failure to parse JSON token buffer is too small");
        }
        return true;
    }
}
