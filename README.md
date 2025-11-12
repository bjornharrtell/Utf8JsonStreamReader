# Utf8JsonStreamReader

A streaming JSON parser based on `System.Text.Json.Utf8JsonReader`.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

### .NET 8

```sh
BenchmarkDotNet v0.15.6, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v4


| Method                                        | Objects | Mean     | Error    | StdDev   |
|---------------------------------------------- |-------- |---------:|---------:|---------:|
| TraverseUtf8JsonStreamReader                  | 100000  | 32.14 ms | 0.212 ms | 0.199 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 32.06 ms | 0.211 ms | 0.197 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 15.18 ms | 0.039 ms | 0.037 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 45.99 ms | 0.271 ms | 0.254 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 64.41 ms | 0.488 ms | 0.407 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 55.38 ms | 0.333 ms | 0.312 ms |
```

### .NET 10

```sh
BenchmarkDotNet v0.15.6, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4


| Method                                        | Objects | Mean     | Error    | StdDev   |
|---------------------------------------------- |-------- |---------:|---------:|---------:|
| TraverseUtf8JsonStreamReader                  | 100000  | 25.88 ms | 0.108 ms | 0.101 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 25.56 ms | 0.126 ms | 0.118 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 11.72 ms | 0.055 ms | 0.051 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 36.60 ms | 0.342 ms | 0.320 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 46.55 ms | 0.210 ms | 0.197 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 44.60 ms | 0.183 ms | 0.172 ms |
```