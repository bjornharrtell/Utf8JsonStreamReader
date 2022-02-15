using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace Wololo.Text.Json
{
    public class Utf8JsonStreamReader
    {
        private readonly int bufferSize;
        private readonly PipeReader pipeReader;

        private bool endOfStream = false;
        private int bytesConsumed = 0;
        private ReadOnlySequence<byte> buffer;
        private JsonReaderState jsonReaderState = new();

        public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;
        public virtual object? Value { get; private set; }

        public Utf8JsonStreamReader(Stream stream, int bufferSize = -1, bool leaveOpen = false)
        {
            this.bufferSize = bufferSize == -1 ? 1024 * 16 : bufferSize;
            pipeReader = PipeReader.Create(stream, new StreamPipeReaderOptions(null, this.bufferSize, this.bufferSize, leaveOpen));
        }

        public async Task<bool> ReadAsync(CancellationToken cancellationToken)
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
            return endOfStream && bytesConsumed == buffer.Length;
        }

        private static string GetString(Utf8JsonReader reader)
        {
            return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
        }

        private static object GetNumber(Utf8JsonReader reader)
        {
            var str = GetString(reader);
            if (str.Length < 3)
                return byte.Parse(str);
            else if (str.Length < 5)
                return short.Parse(str);
            else if (str.Length < 10)
                return int.Parse(str);
            else
                return long.Parse(str);
        }

        private object? GetValue(Utf8JsonReader reader)
        {
            return TokenType switch
            {
                JsonTokenType.PropertyName or JsonTokenType.Comment or JsonTokenType.String => GetString(reader),
                JsonTokenType.Number => GetNumber(reader),
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                _ => null,
            };
        }

        private bool Read()
        {
            var reader = new Utf8JsonReader(buffer.Slice(bytesConsumed), false, jsonReaderState);
            if (!reader.Read())
                return false;
            TokenType = reader.TokenType;
            Value = GetValue(reader);
            bytesConsumed += (int) reader.BytesConsumed;
            jsonReaderState = reader.CurrentState;
            return true;
        }
    }
}