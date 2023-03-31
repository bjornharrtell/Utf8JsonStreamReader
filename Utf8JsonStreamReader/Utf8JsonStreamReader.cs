using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

namespace Wololo.Text.Json
{
    public sealed class Utf8JsonStreamReader : IDisposable
    {
        private readonly int bufferSize;
        private readonly PipeReader pipeReader;

        private bool done = false;
        private bool endOfStream = false;
        private int bytesConsumed = 0;
        private ReadOnlySequence<byte> buffer;
        private JsonReaderState jsonReaderState = new();

        public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;
        public object? Value { get; private set; }

        public Utf8JsonStreamReader(Stream stream, int bufferSize = -1, bool leaveOpen = false)
        {
            this.bufferSize = bufferSize == -1 ? 1024 * 8 : bufferSize;
            pipeReader = PipeReader.Create(stream, new StreamPipeReaderOptions(null, this.bufferSize, this.bufferSize, leaveOpen));
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
            var readResult = await pipeReader.ReadAtLeastAsync(this.bufferSize, cancellationToken);
            buffer = readResult.Buffer;
            bytesConsumed = 0;
            endOfStream = readResult.IsCompleted;
        }

        private bool FinishEarly()
        {
            // if end of stream and all is consumed already we are done
            // this can happen if there is junk data in the stream after the last token
            if (endOfStream && bytesConsumed == buffer.Length)
                return Finish();
            return false;
        }

        private void Advance()
        {
            if (bytesConsumed > 0)
                pipeReader.AdvanceTo(buffer.GetPosition(bytesConsumed));
        }

        private void TryRead() {
            if (buffer.Length > 0 && !Read(endOfStream))
                throw new Exception("Invalid JSON or token too large for buffer");
        }

        private void DetermineDone() {
            // if we are at end of stream and all is consumed we are done
            if (endOfStream && bytesConsumed == buffer.Length)
                done = true;
        }

        public async Task<bool> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (done)
                return !Finish();
            // if first read condition or if read fails
            if (TokenType == JsonTokenType.None || !Read(endOfStream))
            {
                if (FinishEarly())
                    return false;
                Advance();
                await ReadAtLeastAsync(cancellationToken);
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
                if (FinishEarly())
                    return false;
                Advance();
                ReadAtLeastAsync(CancellationToken.None).GetAwaiter().GetResult();
                TryRead();
            }
            DetermineDone();
            return true;
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