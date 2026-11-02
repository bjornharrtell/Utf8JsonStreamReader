using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
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
    const int NoRaw = int.MaxValue;
    const int MaxCachedNames = 1024;
    const int MaxCachedNameBytes = 256;

    readonly Dictionary<ulong, (byte[] Bytes, string Name)> nameCache = [];

    /// <summary>
    /// When true (default) the strings produced for property names are cached, so a name that is repeated for every
    /// object in a large array is only allocated once. The cache is bounded (at most 1024 names of at most 256 bytes)
    /// and does not change the values produced. Only affects <see cref="ToEnumerable(Stream)"/> and
    /// <see cref="ToAsyncEnumerable(Stream, CancellationToken)"/> (and their <c>rawFromDepth</c> overloads).
    /// </summary>
    public bool InternPropertyNames { get; set; } = true;

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

    void ReadStream(Stream stream, OnRead onRead, List<JsonResult>? rawSink = null, int rawFromDepth = NoRaw)
    {
        do
        {
            CopyRemaining();
            readLength = stream.ReadAtLeast(
                buffer.AsSpan(remaining, bufferSize - remaining),
                bufferSize - remaining,
                false
            );
        } while (!ReadBuffer(onRead, rawSink, rawFromDepth, default));
    }

    public void Read(Stream stream, OnRead onRead)
    {
        BeginRead();
        while (!done)
            ReadStream(stream, onRead);
    }

    async ValueTask ReadStreamAsync(
        Stream stream,
        OnRead onRead,
        List<JsonResult>? rawSink = null,
        int rawFromDepth = NoRaw,
        CancellationToken token = default
    )
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
            if (ReadBuffer(onRead, rawSink, rawFromDepth, token))
                break;
        }
    }

    public async ValueTask ReadAsync(Stream stream, OnRead onRead, CancellationToken token = default)
    {
        BeginRead();
        while (!done && !token.IsCancellationRequested)
            await ReadStreamAsync(stream, onRead, token: token);
    }

    static ulong HashName(ReadOnlySpan<byte> name)
    {
        // FNV-1a
        ulong hash = 14695981039346656037UL;
        foreach (var b in name)
            hash = (hash ^ b) * 1099511628211UL;
        return hash;
    }

    string GetPropertyName(ref Utf8JsonReader reader)
    {
        // Escaped or multi-segment names are rare, they are simply not cached
        if (!InternPropertyNames || reader.ValueIsEscaped || reader.HasValueSequence)
            return reader.GetString()!;
        var span = reader.ValueSpan;
        if (span.Length > MaxCachedNameBytes)
            return reader.GetString()!;
        var hash = HashName(span);
        if (nameCache.TryGetValue(hash, out var entry))
            return span.SequenceEqual(entry.Bytes) ? entry.Name : reader.GetString()!; // else hash collision
        var name = reader.GetString()!;
        if (nameCache.Count < MaxCachedNames)
            nameCache[hash] = (span.ToArray(), name);
        return name;
    }

    void AccumulateResults(ref Utf8JsonReader reader, List<JsonResult> results)
    {
        var value =
            reader.TokenType == JsonTokenType.PropertyName
                ? GetPropertyName(ref reader)
                : Utf8JsonHelpers.GetValue(ref reader);
        results.Add(new JsonResult(reader.TokenType, value) { Depth = reader.CurrentDepth });
    }

    static void ValidateRawFromDepth(int rawFromDepth) => ArgumentOutOfRangeException.ThrowIfNegative(rawFromDepth);

    public IEnumerable<JsonResult> ToEnumerable(Stream stream) => ToEnumerable(stream, NoRaw);

    /// <summary>
    /// Like <see cref="ToEnumerable(Stream)"/> but every object or array starting at <paramref name="rawFromDepth"/>
    /// or deeper (see <see cref="JsonResult.Depth"/>) is returned as a single result holding its complete raw JSON text
    /// (see <see cref="JsonResult.IsRaw"/>) instead of one result per token. For example, with a root array of
    /// records, <c>rawFromDepth: 2</c> returns each record property that is an object or array as raw JSON.
    /// A raw value must fit in the maximum buffer size.
    /// </summary>
    public IEnumerable<JsonResult> ToEnumerable(Stream stream, int rawFromDepth)
    {
        ValidateRawFromDepth(rawFromDepth);
        BeginRead();
        var results = new List<JsonResult>();
        void onRead(ref Utf8JsonReader r) => AccumulateResults(ref r, results);
        var rawSink = rawFromDepth == NoRaw ? null : results;
        while (!done)
        {
            ReadStream(stream, onRead, rawSink, rawFromDepth);
            foreach (var item in results)
                yield return item;
            results.Clear();
        }
    }

    public IAsyncEnumerable<JsonResult> ToAsyncEnumerable(Stream stream, CancellationToken token = default) =>
        ToAsyncEnumerable(stream, NoRaw, token);

    /// <summary>
    /// Async version of <see cref="ToEnumerable(Stream, int)"/>.
    /// </summary>
    public async IAsyncEnumerable<JsonResult> ToAsyncEnumerable(
        Stream stream,
        int rawFromDepth,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        ValidateRawFromDepth(rawFromDepth);
        BeginRead();
        var results = new List<JsonResult>();
        void onRead(ref Utf8JsonReader r) => AccumulateResults(ref r, results);
        var rawSink = rawFromDepth == NoRaw ? null : results;
        while (!done && !token.IsCancellationRequested)
        {
            await ReadStreamAsync(stream, onRead, rawSink, rawFromDepth, token);
            foreach (var item in results)
            {
                if (token.IsCancellationRequested)
                    yield break;
                yield return item;
            }
            results.Clear();
        }
    }

    private bool ReadBuffer(
        OnRead onRead,
        List<JsonResult>? rawSink,
        int rawFromDepth,
        CancellationToken token = default
    )
    {
        bufferLength = readLength + remaining;
        offset = 0;
        done = bufferLength < bufferSize;
        var reader = new Utf8JsonReader(buffer.AsSpan(0, bufferLength), done, jsonReaderState);
        if (rawSink is null)
        {
            while (reader.Read() && !token.IsCancellationRequested)
                onRead(ref reader);
        }
        else
        {
            while (!token.IsCancellationRequested)
            {
                // Checkpoint, so that a subtree that continues in the next read can be retried from its start token
                var before = reader;
                if (!reader.Read())
                    break;
                if (
                    reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray
                    && reader.CurrentDepth >= rawFromDepth
                )
                {
                    var skipped = reader;
                    if (skipped.TrySkip())
                    {
                        var start = (int)reader.TokenStartIndex;
                        var raw = Encoding.UTF8.GetString(buffer.AsSpan(start, (int)skipped.BytesConsumed - start));
                        rawSink.Add(new JsonResult(reader.TokenType, raw) { Depth = reader.CurrentDepth });
                        reader = skipped;
                        continue;
                    }
                    // Not all of it is buffered yet: rewind to before the start token. When that makes no progress
                    // at all the buffer is grown below, so it will eventually fit (or fail at maxBufferSize).
                    reader = before;
                    break;
                }
                onRead(ref reader);
            }
        }
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
