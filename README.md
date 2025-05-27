# Utf8JsonStreamReader

A streaming JSON parser based on `System.Text.Json.Utf8JsonReader`.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

```sh
BenchmarkDotNet v0.15.0, Linux Ubuntu 24.04.2 LTS (Noble Numbat)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 5.13GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.116
  [Host]     : .NET 8.0.16 (8.0.1625.21506), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.16 (8.0.1625.21506), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                                        | Objects | Mean     | Error    | StdDev   |
|---------------------------------------------- |-------- |---------:|---------:|---------:|
| TraverseUtf8JsonStreamReader                  | 100000  | 32.10 ms | 0.142 ms | 0.126 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 32.32 ms | 0.197 ms | 0.175 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 15.74 ms | 0.084 ms | 0.078 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 45.51 ms | 0.538 ms | 0.503 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 62.60 ms | 1.232 ms | 1.767 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 53.90 ms | 0.955 ms | 0.846 ms |
```
