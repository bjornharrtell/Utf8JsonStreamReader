using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wololo.Text.Json;

namespace Tests
{
    [TestClass]
    public class BufferDebuggingTest
    {
        [TestMethod]
        public void SmallTokenThatShouldWork()
        {
            // Small property name that should definitely work
            var propertyName = new string('x', 1000); // 1KB
            var validJson = $@"{{""{ propertyName }"": ""value""}}";
            var jsonBytes = Encoding.UTF8.GetBytes(validJson);
            
            Console.WriteLine($"JSON size: {jsonBytes.Length} bytes");
            
            var stream = new MemoryStream(jsonBytes);
            var reader = new Utf8JsonStreamReader(500); // Start smaller than the JSON
            var tokens = new List<JsonTokenType>();
            
            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                Console.WriteLine($"Token: {r.TokenType}");
                tokens.Add(r.TokenType);
            });
            
            Assert.AreEqual(4, tokens.Count);
        }

        [TestMethod] 
        public void Medium10MBPreallocatedShouldWork()
        {
            // This should match your working downstream case with 10MB buffer
            var propertyName = new string('x', 5 * 1024 * 1024); // 5MB property name
            var validJson = $@"{{""{ propertyName }"": ""value""}}";
            var jsonBytes = Encoding.UTF8.GetBytes(validJson);
            
            Console.WriteLine($"JSON size: {jsonBytes.Length} bytes");
            
            var stream = new MemoryStream(jsonBytes); // Regular memory stream, not chunked
            var reader = new Utf8JsonStreamReader(10 * 1024 * 1024); // Pre-allocate 10MB like downstream
            var tokens = new List<JsonTokenType>();
            
            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                Console.WriteLine($"Token: {r.TokenType}");
                tokens.Add(r.TokenType);
            });
            
            Assert.AreEqual(4, tokens.Count);
        }

        [TestMethod] 
        public void Medium10MBPreallocatedWithChunksFailsWhy()
        {
            // Same as above but with chunks - this should reveal the issue
            var propertyName = new string('x', 5 * 1024 * 1024); // 5MB property name
            var validJson = $@"{{""{ propertyName }"": ""value""}}";
            var jsonBytes = Encoding.UTF8.GetBytes(validJson);
            
            Console.WriteLine($"JSON size: {jsonBytes.Length} bytes");
            
            var stream = new ChunkedStream(jsonBytes, 8192); // Same as failing test
            var reader = new Utf8JsonStreamReader(10 * 1024 * 1024); // Pre-allocate 10MB
            var tokens = new List<JsonTokenType>();
            
            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                Console.WriteLine($"Token: {r.TokenType}");
                tokens.Add(r.TokenType);
            });
            
            Assert.AreEqual(4, tokens.Count);
        }

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
                    return 0; // EOF
                
                int toRead = Math.Min(count, Math.Min(_chunkSize, _data.Length - _position));
                Array.Copy(_data, _position, buffer, offset, toRead);
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