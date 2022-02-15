using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Wololo.Text.Json;

namespace Benchmarks
{
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
                        Num = 13434934.23233434
                    };;
                var containerObject = new
                {
                    Array = elements
                };
                json = JsonSerializer.Serialize(
                    containerObject,
                    new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = false
                    });
            }

            [Benchmark]
            public async Task TraverseUtf8JsonStreamReader()
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                var reader = new Utf8JsonStreamReader(stream);
                while (await reader.ReadAsync() == true)
                {
                    _ = reader.TokenType;
                    _ = reader.Value;
                }
            }

            [Benchmark]
            public async Task TraverseUtf8JsonStreamTokenEnumerator()
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await foreach (var result in new Utf8JsonStreamTokenEnumerator(stream))
                {
                    _ = result.TokenType;
                    _ = result.Value;
                }
            }

            [Benchmark]
            public async Task TraverseJsonTextReader()
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                var reader = new Newtonsoft.Json.JsonTextReader(new StreamReader(stream));
                while (await reader.ReadAsync() == true)
                {
                    _ = reader.TokenType;
                    _ = reader.Value;
                }
            }
        }

        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}