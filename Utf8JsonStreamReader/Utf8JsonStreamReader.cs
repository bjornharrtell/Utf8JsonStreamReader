using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

namespace Wololo.Text.Json
{
    public sealed class Utf8JsonStreamReader : IDisposable
    {
        private readonly int bufferSize;
        private readonly PipeReader pipeReader;

        private bool endOfStream = false;
        private int bytesConsumed = 0;
        private ReadOnlySequence<byte> buffer;
        private JsonReaderState jsonReaderState = new();

        public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;
        public object? Value { get; private set; }

        public Utf8JsonStreamReader(Stream stream, int bufferSize = -1, bool leaveOpen = false)
        {
            this.bufferSize = bufferSize == -1 ? 1024 * 16 : bufferSize;
            pipeReader = PipeReader.Create(stream, new StreamPipeReaderOptions(null, this.bufferSize, this.bufferSize, leaveOpen));
        }

        public async Task<bool> ReadAsync(CancellationToken cancellationToken = default)
        {
            // TODO: possibly go straight to read from pipe if remaining buffer length is smaller than set threshold
            if (TokenType == JsonTokenType.None || !Read())
            {
                if (bytesConsumed > 0)
                    pipeReader.AdvanceTo(buffer.GetPosition(bytesConsumed));
                var readResult = await pipeReader.ReadAtLeastAsync(this.bufferSize, cancellationToken);
                buffer = readResult.Buffer;
                bytesConsumed = 0;
                endOfStream = readResult.IsCompleted;
                if (!Read())
                    throw new Exception("Invalid JSON or token too large for buffer");
            }
            return !(endOfStream && bytesConsumed == buffer.Length);
        }

        private bool Read()
        {
            var reader = new Utf8JsonReader(buffer.Slice(bytesConsumed), false, jsonReaderState);
            if (!reader.Read())
                return false;
            TokenType = reader.TokenType;
            Value = Utf8JsonHelpers.GetValue(reader);
            bytesConsumed += (int) reader.BytesConsumed;
            jsonReaderState = reader.CurrentState;
            return true;
        }

        public void Dispose()
        {
            pipeReader.Complete();
        }
    }
}