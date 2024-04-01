using System.Collections;
using System.Text.Json;

namespace Wololo.Text.Json;

public sealed class Utf8JsonStreamTokenEnumerable : IEnumerable<JsonResult>
{
    private readonly int bufferSize;
    private readonly Stream stream;
    private readonly JsonResult[] resultBuffer;
    private readonly Memory<byte> buffer;
    private int bufferLength = 0;
    private int offset = 0;
    private int resultsLength = 0;
    private JsonReaderState jsonReaderState = new();

    public Utf8JsonStreamTokenEnumerable(Stream stream, int bufferSize = -1)
    {
        this.bufferSize = bufferSize == -1 ? 1024 * 8 : bufferSize;
        this.stream = stream;
        buffer = new byte[this.bufferSize];
        resultBuffer = new JsonResult[this.bufferSize];
    }

    public IEnumerator<JsonResult> GetEnumerator()
    {
        var done = false;
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = stream.ReadAtLeast(buffer[remaining..].Span, bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            ReadTokens(done);
            for (int i = 0; i < resultsLength; i++)
                yield return resultBuffer[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void ReadTokens(bool isFinalBlock)
    {
        var reader = new Utf8JsonReader(buffer[offset..bufferLength].Span, isFinalBlock, jsonReaderState);
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