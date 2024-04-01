using System.Text.Json;

namespace Wololo.Text.Json;

public sealed class Utf8JsonStreamReader : IDisposable
{
    private readonly int bufferSize;
    private readonly Stream stream;

    private bool done = false;
    private bool endOfStream = false;
    private int bytesConsumed = 0;
    private Memory<byte> buffer;
    private int bufferLength = 0;
    private JsonReaderState jsonReaderState = new();

    public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;
    public object? Value { get; private set; }

    public Utf8JsonStreamReader(Stream stream, int bufferSize = -1, bool leaveOpen = false)
    {
        this.bufferSize = bufferSize == -1 ? 1024 * 8 : bufferSize;
        this.buffer = new byte[this.bufferSize];
        this.stream = stream;
    }

    private bool Finish()
    {
        TokenType = JsonTokenType.None;
        Value = null;
        done = true;
        return true;
    }

    private async Task ReadAtLeastAsync(CancellationToken cancellationToken)
    {
        var remaining = bufferLength - bytesConsumed;
        if (remaining > 0)
            buffer.Slice(bytesConsumed).CopyTo(buffer);
        var readLength = await stream.ReadAtLeastAsync(buffer.Slice(remaining), this.bufferSize - remaining, false);
        bufferLength = readLength + remaining;
        bytesConsumed = 0;
        endOfStream = bufferLength < this.bufferSize;
    }

    private void ReadAtLeast()
    {
        var remaining = bufferLength - bytesConsumed;
        if (remaining > 0)
            buffer.Slice(bytesConsumed).CopyTo(buffer);
        var readLength = stream.ReadAtLeast(buffer.Slice(remaining).Span, this.bufferSize - remaining, false);
        bufferLength = readLength + remaining;
        bytesConsumed = 0;
        endOfStream = bufferLength < this.bufferSize;
    }

    private bool FinishEarly()
    {
        // if end of stream and all is consumed already we are done
        // this can happen if there is junk data in the stream after the last token
        if (endOfStream && bytesConsumed == bufferLength)
            return Finish();
        return false;
    }

    private void TryRead()
    {
        if (bufferLength > 0 && !Read(endOfStream))
            throw new Exception("Invalid JSON or token too large for buffer");
    }

    private void DetermineDone()
    {
        // if we are at end of stream and all is consumed we are done
        if (endOfStream && bytesConsumed == bufferLength)
            done = true;
    }

    public async Task<bool> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (done)
            return !Finish();
        // if first read condition or if read fails
        if (TokenType == JsonTokenType.None || !Read(endOfStream))
        {
            await ReadAtLeastAsync(cancellationToken);
            if (FinishEarly())
                return false;
            TryRead();
        }
        DetermineDone();
        return true;
    }

    public bool Read()
    {
        if (done)
            return !Finish();
        // if first read condition or if read fails
        if (TokenType == JsonTokenType.None || !Read(endOfStream))
        {
            ReadAtLeast();
            if (FinishEarly())
                return false;
            TryRead();
        }
        DetermineDone();
        return true;
    }

    private bool Read(bool isFinalBlock)
    {
        var reader = new Utf8JsonReader(buffer.Slice(bytesConsumed, bufferLength - bytesConsumed).Span, isFinalBlock, jsonReaderState);
        var result = reader.Read();
        bytesConsumed += (int)reader.BytesConsumed;
        jsonReaderState = reader.CurrentState;
        if (!result)
            return false;
        TokenType = reader.TokenType;
        Value = Utf8JsonHelpers.GetValue(ref reader);
        return true;
    }

    public void Dispose()
    {
        stream.Dispose();
    }
}