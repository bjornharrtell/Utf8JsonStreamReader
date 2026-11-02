using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Wololo.Text.Json;

namespace Benchmarks;

public class Program
{
    public class TraverseBenchmark
    {
        string json;

        [Params(100000)]
        public int Objects;

        [GlobalSetup]
        public void Setup()
        {
            var elements = new object[Objects];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new
                {
                    Id = 2,
                    NegativeId = -23,
                    TimeStamp = "2012-10-21T00:00:00+05:30",
                    Status = false,
                    Num = 13434934.23233434,
                };
            var containerObject = new { Array = elements };
            json = JsonSerializer.Serialize(
                containerObject,
                new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = false,
                }
            );
        }

        [Benchmark]
        public void TraverseUtf8JsonStreamReader()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader();
            reader.Read(
                stream,
                (ref Utf8JsonReader reader) =>
                {
                    _ = reader.TokenType;
                    _ = Utf8JsonHelpers.GetValue(ref reader);
                }
            );
        }

        [Benchmark]
        public async Task TraverseUtf8JsonStreamReaderAsync()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader();
            await reader.ReadAsync(
                stream,
                (ref Utf8JsonReader reader) =>
                {
                    _ = reader.TokenType;
                    _ = Utf8JsonHelpers.GetValue(ref reader);
                }
            );
        }

        [Benchmark]
        public void TraverseUtf8JsonStreamReaderRawValue()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader();
            reader.Read(
                stream,
                (ref Utf8JsonReader reader) =>
                {
                    _ = reader.TokenType;
                    _ = reader.ValueSpan;
                }
            );
        }

        [Benchmark]
        public void TraverseUtf8JsonStreamReaderToEnumerable()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader();
            foreach (var item in reader.ToEnumerable(stream))
            {
                _ = item.TokenType;
                _ = item.Value;
            }
        }

        [Benchmark]
        public async Task TraverseUtf8JsonStreamReaderToAsyncEnumerable()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonStreamReader();
            await foreach (var item in reader.ToAsyncEnumerable(stream))
            {
                _ = item.TokenType;
                _ = item.Value;
            }
        }

        [Benchmark]
        public void TraverseNewtonsoftJsonTextReader()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var reader = new Newtonsoft.Json.JsonTextReader(new StreamReader(stream));
            while (reader.Read())
            {
                _ = reader.TokenType;
                _ = reader.Value;
            }
        }
    }

    public class NestedRecordsBenchmark
    {
        byte[] bytes;

        [Params(50000)]
        public int Records;

        [GlobalSetup]
        public void Setup()
        {
            var elements = new object[Records];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new
                {
                    Id = i,
                    Name = "Record " + i,
                    Status = false,
                    Geometry = new { Type = "Point", Coordinates = new[] { 12.5, 55.7 } },
                    Tags = new[] { "a", "b", "c" },
                };
            bytes = JsonSerializer.SerializeToUtf8Bytes(elements, new JsonSerializerOptions { WriteIndented = false });
        }

        [Benchmark(Baseline = true)]
        public int TokensInternNames()
        {
            var reader = new Utf8JsonStreamReader();
            int n = 0;
            foreach (var item in reader.ToEnumerable(new MemoryStream(bytes)))
                n += item.Value is string s ? s.Length : 1;
            return n;
        }

        [Benchmark]
        public int TokensNoInternNames()
        {
            var reader = new Utf8JsonStreamReader { InternPropertyNames = false };
            int n = 0;
            foreach (var item in reader.ToEnumerable(new MemoryStream(bytes)))
                n += item.Value is string s ? s.Length : 1;
            return n;
        }

        [Benchmark]
        public int RawSubtrees()
        {
            var reader = new Utf8JsonStreamReader();
            int n = 0;
            foreach (var item in reader.ToEnumerable(new MemoryStream(bytes), rawFromDepth: 2))
                n += item.Value is string s ? s.Length : 1;
            return n;
        }

        [Benchmark]
        public async Task<int> RawSubtreesAsync()
        {
            var reader = new Utf8JsonStreamReader();
            int n = 0;
            await foreach (var item in reader.ToAsyncEnumerable(new MemoryStream(bytes), rawFromDepth: 2))
                n += item.Value is string s ? s.Length : 1;
            return n;
        }
    }

    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance;
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
