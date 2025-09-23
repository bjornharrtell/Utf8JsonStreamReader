using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wololo.Text.Json;

namespace Tests
{
    [TestClass]
    public class DownstreamReproductionTest
    {
        [TestMethod]
        public void LargeTokenWithSmallChunksAndSmallInitialBuffer()
        {
            // This test tries to reproduce the downstream failure:
            // - Large JSON token (e.g., 5MB property name)
            // - Small initial buffer (32KB default)
            // - Data comes in small chunks like from network
            // - Should work when manually set to 10MB buffer
            
            var largePropertyName = new string('x', 5 * 1024 * 1024); // 5MB property name
            var validJson = $@"{{""{ largePropertyName }"": ""value""}}";
            var jsonBytes = Encoding.UTF8.GetBytes(validJson);
            
            // Simulate network stream with small chunks
            var stream = new ChunkedStream(jsonBytes, chunkSize: 8192); // 8KB chunks
            
            // Start with small buffer - this should auto-grow to handle the large token
            var reader = new Utf8JsonStreamReader(32 * 1024); // 32KB initial, 1GB max default
            var tokens = new List<JsonTokenType>();
            
            // This should work by auto-growing the buffer
            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                tokens.Add(r.TokenType);
            });
            
            // Verify we got the expected tokens
            Assert.AreEqual(4, tokens.Count);
            Assert.AreEqual(JsonTokenType.StartObject, tokens[0]);
            Assert.AreEqual(JsonTokenType.PropertyName, tokens[1]);
            Assert.AreEqual(JsonTokenType.String, tokens[2]);
            Assert.AreEqual(JsonTokenType.EndObject, tokens[3]);
        }

        [TestMethod]
        public void LargeTokenWorksWithPreAllocatedBuffer()
        {
            // This test verifies that the same scenario works when we pre-allocate
            // a 10MB buffer (as mentioned working in downstream)
            
            var largePropertyName = new string('x', 5 * 1024 * 1024); // 5MB property name  
            var validJson = $@"{{""{ largePropertyName }"": ""value""}}";
            var jsonBytes = Encoding.UTF8.GetBytes(validJson);
            
            // Simulate network stream with small chunks
            var stream = new ChunkedStream(jsonBytes, chunkSize: 8192); // 8KB chunks
            
            // Pre-allocate 10MB buffer like the working downstream case
            var reader = new Utf8JsonStreamReader(10 * 1024 * 1024); // 10MB buffer
            var tokens = new List<JsonTokenType>();
            
            // This should definitely work
            reader.Read(stream, (ref Utf8JsonReader r) =>
            {
                tokens.Add(r.TokenType);
            });
            
            // Verify we got the expected tokens
            Assert.AreEqual(4, tokens.Count);
            Assert.AreEqual(JsonTokenType.StartObject, tokens[0]);
            Assert.AreEqual(JsonTokenType.PropertyName, tokens[1]);
            Assert.AreEqual(JsonTokenType.String, tokens[2]);
            Assert.AreEqual(JsonTokenType.EndObject, tokens[3]);
        }

        // Simulates a network stream that delivers data in fixed-size chunks
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
                
                // Limit read to chunk size and remaining data
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