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
| TraverseUtf8JsonStreamReader                  | 100000  | 31.99 ms | 0.180 ms | 0.160 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 33.27 ms | 0.652 ms | 0.610 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 15.81 ms | 0.103 ms | 0.096 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 44.29 ms | 0.319 ms | 0.283 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 62.25 ms | 1.191 ms | 1.463 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 51.39 ms | 0.289 ms | 0.256 ms |
```
