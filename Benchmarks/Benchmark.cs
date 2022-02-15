using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using Wololo.Text.Json;

namespace Benchmarks
{
    public class Program
    {
        //[SimpleJob(RuntimeMoniker.CoreRt31)]
        //[SimpleJob(RuntimeMoniker.CoreRt50)]
        public class TraverseBenchmark
        {
            string json;

            //[Params(2, 20, 200, 20000)]
            [Params(2000)]
            //[Params(20000)]
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
                //int c = 0;
                while (await reader.ReadAsync(CancellationToken.None) == true)
                {
                    _ = reader.TokenType;
                    //if (reader.TokenType == JsonTokenType.String)
                    //    c++;
                }
            }

            [Benchmark]
            public async Task TraverseJsonTextReader()
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                var reader = new Newtonsoft.Json.JsonTextReader(new StreamReader(stream));
                //int c = 0;
                while (await reader.ReadAsync(CancellationToken.None) == true)
                {
                    _ = reader.TokenType;
                    //if (reader.TokenType == Newtonsoft.Json.JsonToken.String)
                    //    c++;
                }
            }
        }

        public static void Main(string[] args)
        {
            //var summaryStyle = new BenchmarkDotNet.Reports.SummaryStyle(null, false, SizeUnit.B, TimeUnit.Microsecond);
            //var config = DefaultConfig.Instance.WithSummaryStyle(summaryStyle);
            var config = DefaultConfig.Instance;
            //var config = new DebugInProcessConfig();
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}