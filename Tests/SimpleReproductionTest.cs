using System.Text;
using System.Text.Json;
using Wololo.Text.Json;

namespace Tests
{
    [TestClass]
    public class SimpleReproductionTest
    {
        [TestMethod]
        public void SimplePropertyNameInChunks()
        {
            // Very simple test: property name that spans multiple small chunks
            var propertyName = new string('x', 1000); // Just 1KB property name
            var validJson = $@"{{""{propertyName}"": ""value""}}";
            var jsonBytes = Encoding.UTF8.GetBytes(validJson);

            Console.WriteLine($"JSON size: {jsonBytes.Length} bytes");

            // Very small chunks to force multiple reads
            var stream = new ChunkedStream(jsonBytes, chunkSize: 100); // 100 byte chunks

            // Small initial buffer
            var reader = new Utf8JsonStreamReader(200); // 200 byte buffer
            var tokens = new List<JsonTokenType>();

            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                Console.WriteLine($"Token: {r.TokenType}");
                tokens.Add(r.TokenType);
            });

            // Should get: StartObject, PropertyName, String, EndObject  
            Assert.HasCount(4, tokens);
        }

        [TestMethod]
        public void DiagnosticTest()
        {
            // Simple JSON to understand the buffer behavior
            var json = @"{""test"": ""value""}";
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var stream = new ChunkedStream(jsonBytes, chunkSize: 5); // Very small chunks

            var reader = new Utf8JsonStreamReader(50); // Small buffer
            var tokens = new List<JsonTokenType>();

            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                tokens.Add(r.TokenType);
            });

            Assert.HasCount(4, tokens);
        }

        // Same ChunkedStream as before
        private class ChunkedStream : Stream
        {
            private readonly byte[] _data;
            private readonly int _chunkSize;
            private int _position = 0;

            public ChunkedStream(byte[] data, int chunkSize)
            {
                _data = data;
                _chunkSize = chunkSize;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _data.Length;
            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_position >= _data.Length)
                {
                    Console.WriteLine("ChunkedStream: EOF reached");
                    return 0; // EOF
                }

                int toRead = Math.Min(count, Math.Min(_chunkSize, _data.Length - _position));
                Array.Copy(_data, _position, buffer, offset, toRead);
                Console.WriteLine($"ChunkedStream: Read {toRead} bytes at position {_position}");
                _position += toRead;
                return toRead;
            }

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}