# Utf8JsonStreamReader

![NuGet Version](https://img.shields.io/nuget/v/Utf8JsonStreamReader)
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
| TraverseUtf8JsonStreamReader                  | 100000  | 27.38 ms | 0.120 ms | 0.112 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 27.09 ms | 0.186 ms | 0.174 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 13.91 ms | 0.080 ms | 0.075 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 35.33 ms | 0.116 ms | 0.138 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 48.09 ms | 0.292 ms | 0.273 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 45.86 ms | 0.206 ms | 0.183 ms 
```