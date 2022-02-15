using System.Buffers;
using System.IO.Pipelines;
using System.Text;
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
            return !(endOfStream && bytesConsumed == buffer.Length);
        }

        private static string GetString(Utf8JsonReader reader)
        {
            return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
        }

        private static object GetDecimal(string str)
        {
            var value = double.Parse(str);
            // TODO: check if value can be losslessly converted to float?
            return value;
        }

        private static object GetInteger(string str)
        {
            var value = long.Parse(str);
            if (short.MinValue < value && value < short.MaxValue)
                return (short) value;
            if (int.MinValue < value && value < int.MaxValue)
                return (int) value;
            return value;
        }

        private static object GetNumber(Utf8JsonReader reader)
        {
            var str = GetString(reader);
            if (str.Contains('.'))
                return GetDecimal(str);
            else
                return GetInteger(str);
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

        public void Dispose()
        {
            pipeReader.Complete();
        }
    }
}