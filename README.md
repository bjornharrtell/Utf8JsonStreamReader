# Utf8JsonStreamReader

A streaming JSON parser based on `System.Text.Json.Utf8JsonReader`.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

```sh
BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.110
  [Host]     : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v4


| Method                                        | Objects | Mean     | Error    | StdDev   |
|---------------------------------------------- |-------- |---------:|---------:|---------:|
| TraverseUtf8JsonStreamReader                  | 100000  | 35.61 ms | 0.273 ms | 0.255 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 36.27 ms | 0.185 ms | 0.164 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 17.65 ms | 0.146 ms | 0.129 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 49.81 ms | 0.454 ms | 0.425 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 70.46 ms | 0.200 ms | 0.187 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 58.34 ms | 0.194 ms | 0.182 ms |
```