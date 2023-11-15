using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

namespace Wololo.Text.Json;

public readonly record struct JsonResult(JsonTokenType TokenType = JsonTokenType.None, object? Value = null);

public sealed class Utf8JsonStreamTokenEnumerator : IAsyncEnumerable<JsonResult>
{
    private readonly int bufferSize;
    private readonly PipeReader pipeReader;
    private readonly JsonResult[] resultBuffer;

    private JsonReaderState jsonReaderState = new();
    private ReadOnlySequence<byte> buffer;
    private int offset = 0;
    private int resultsLength = 0;

    public Utf8JsonStreamTokenEnumerator(Stream stream, int bufferSize = -1, bool leaveOpen = false)
    {
        this.bufferSize = bufferSize == -1 ? 1024 * 8 : bufferSize;
        resultBuffer = new JsonResult[this.bufferSize];
        pipeReader = PipeReader.Create(stream, new StreamPipeReaderOptions(null, this.bufferSize, this.bufferSize, leaveOpen));
    }

    public async IAsyncEnumerator<JsonResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var done = false;
        while (!done)
        {
            if (offset > 0)
                pipeReader.AdvanceTo(buffer.GetPosition(offset));
            var readResult = await pipeReader.ReadAtLeastAsync(bufferSize, cancellationToken);
            buffer = readResult.Buffer;
            offset = 0;
            if (readResult.IsCompleted)
                done = true;
            ReadTokens(done);
            for (int i = 0; i < resultsLength; i++)
                yield return resultBuffer[i];
        }
    }

    private void ReadTokens(bool isFinalBlock)
    {
        var reader = new Utf8JsonReader(buffer, isFinalBlock, jsonReaderState);
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