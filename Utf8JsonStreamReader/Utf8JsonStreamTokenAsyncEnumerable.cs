using System.Text.Json;

namespace Wololo.Text.Json;

public readonly record struct JsonResult(JsonTokenType TokenType = JsonTokenType.None, object? Value = null);

public sealed class Utf8JsonStreamTokenAsyncEnumerable : IAsyncEnumerable<JsonResult>
{
    private readonly int bufferSize;
    private readonly Stream stream;
    private readonly JsonResult[] resultBuffer;

    private JsonReaderState jsonReaderState = new();
    private Memory<byte> buffer;
    private int bufferLength = 0;
    private int offset = 0;
    private int resultsLength = 0;

    public Utf8JsonStreamTokenAsyncEnumerable(Stream stream, int bufferSize = -1)
    {
        this.bufferSize = bufferSize == -1 ? 1024 * 8 : bufferSize;
        this.buffer = new byte[this.bufferSize];
        resultBuffer = new JsonResult[this.bufferSize];
        this.stream = stream;
    }

    public async IAsyncEnumerator<JsonResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var done = false;
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer.Slice(offset).CopyTo(buffer);
            var readLength = await stream.ReadAtLeastAsync(buffer.Slice(remaining), this.bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < this.bufferSize;
            ReadTokens(done);
            for (int i = 0; i < resultsLength; i++)
                yield return resultBuffer[i];
        }
    }

    private void ReadTokens(bool isFinalBlock)
    {
        var reader = new Utf8JsonReader(buffer.Slice(offset, bufferLength - offset).Span, isFinalBlock, jsonReaderState);
        int i = 0;
        while (reader.Read())
        {
            jsonReaderState = reader.CurrentState;
            resultBuffer[i++] = new JsonResult(reader.TokenType, Utf8JsonHelpers.GetValue(ref reader));
        }
        offset = (int)reader.BytesConsumed;
        resultsLength = i;
    }
}