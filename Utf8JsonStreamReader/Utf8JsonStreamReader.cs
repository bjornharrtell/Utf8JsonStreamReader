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
            if (TokenType == JsonTokenType.None || !Read(endOfStream))
            {
                if (bytesConsumed > 0)
                    pipeReader.AdvanceTo(buffer.GetPosition(bytesConsumed));
                var readResult = await pipeReader.ReadAtLeastAsync(this.bufferSize, cancellationToken);
                buffer = readResult.Buffer;
                bytesConsumed = 0;
                endOfStream = readResult.IsCompleted;
                if ((buffer.Length - bytesConsumed) > 0 && !Read(endOfStream))
                    throw new Exception("Invalid JSON or token too large for buffer");
            }
            return !(endOfStream && bytesConsumed == buffer.Length);
        }

        public bool Read()
        {
            if (TokenType == JsonTokenType.None || !Read(endOfStream))
            {
                if (bytesConsumed > 0)
                    pipeReader.AdvanceTo(buffer.GetPosition(bytesConsumed));
                var readResult = pipeReader.ReadAtLeastAsync(this.bufferSize, CancellationToken.None).AsTask().GetAwaiter().GetResult();
                buffer = readResult.Buffer;
                bytesConsumed = 0;
                endOfStream = readResult.IsCompleted;
                if ((buffer.Length - bytesConsumed) > 0 && !Read(endOfStream))
                    throw new Exception("Invalid JSON or token too large for buffer");
            }
            return !(endOfStream && bytesConsumed == buffer.Length);
        }

        private bool Read(bool isFinalBlock)
        {
            var reader = new Utf8JsonReader(buffer.Slice(bytesConsumed), isFinalBlock, jsonReaderState);
            var result = reader.Read();
            bytesConsumed += (int) reader.BytesConsumed;
            jsonReaderState = reader.CurrentState;
            if (!result)
                return false;
            TokenType = reader.TokenType;
            Value = Utf8JsonHelpers.GetValue(ref reader);
            return true;
        }

        public void Dispose()
        {
            pipeReader.Complete();
        }
    }
}