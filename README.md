# Utf8JsonStreamReader

![NuGet Version](https://img.shields.io/nuget/v/Utf8StreamReader)
[![Coverage Status](https://coveralls.io/repos/github/bjornharrtell/Utf8JsonStreamReader/badge.svg?branch=main)](https://coveralls.io/github/bjornharrtell/Utf8JsonStreamReader?branch=main)

A streaming JSON parser based on `System.Text.Json.Utf8JsonReader`.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

### .NET 10

```sh
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v4


| Method                                        | Objects | Mean     | Error    | StdDev   |
|---------------------------------------------- |-------- |---------:|---------:|---------:|
| TraverseUtf8JsonStreamReader                  | 100000  | 26.66 ms | 0.130 ms | 0.115 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 26.57 ms | 0.143 ms | 0.127 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 12.72 ms | 0.065 ms | 0.058 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 37.53 ms | 0.422 ms | 0.394 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 45.46 ms | 0.261 ms | 0.231 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 44.50 ms | 0.206 ms | 0.183 ms |
```