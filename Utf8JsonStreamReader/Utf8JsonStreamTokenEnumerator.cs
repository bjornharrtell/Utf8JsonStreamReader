using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

namespace Wololo.Text.Json
{
    public struct JsonResult
    {
        public JsonTokenType TokenType { get; set; } = JsonTokenType.None;
        public object? Value { get; set; }
    }

    public sealed class Utf8JsonStreamTokenEnumerator : IAsyncEnumerable<JsonResult>
    {
        private readonly int bufferSize;
        private readonly PipeReader pipeReader;
        private readonly List<JsonResult> resultBuffer = new(1024 * 8);

        private JsonReaderState jsonReaderState = new();
        private ReadOnlySequence<byte> buffer;
        private int offset = 0;

        public Utf8JsonStreamTokenEnumerator(Stream stream, int bufferSize = -1, bool leaveOpen = false)
        {
            this.bufferSize = bufferSize == -1 ? 1024 * 8 : bufferSize;
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
                ReadTokens();
                foreach (var result in resultBuffer)
                    yield return result;
            }
        }

        private void ReadTokens()
        {
            resultBuffer.Clear();
            var reader = new Utf8JsonReader(buffer, false, jsonReaderState);
            while (reader.Read())
            {
                jsonReaderState = reader.CurrentState;
                resultBuffer.Add(new JsonResult() {
                    TokenType = reader.TokenType,
                    Value = Utf8JsonHelpers.GetValue(reader)
                });
            }
            offset = (int) reader.BytesConsumed;
        }
    }
}