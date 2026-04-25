# Utf8JsonStreamReader

![NuGet Version](https://img.shields.io/nuget/v/Utf8JsonStreamReader)
[![Coverage Status](https://coveralls.io/repos/github/bjornharrtell/Utf8JsonStreamReader/badge.svg?branch=main)](https://coveralls.io/github/bjornharrtell/Utf8JsonStreamReader?branch=main)

A streaming JSON parser based on `System.Text.Json.Utf8JsonReader`.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

### .NET 10

```sh
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 (Resolute Raccoon)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4


| Method                                        | Objects | Mean     | Error    | StdDev   |
|---------------------------------------------- |-------- |---------:|---------:|---------:|
| TraverseUtf8JsonStreamReader                  | 100000  | 26.77 ms | 0.118 ms | 0.110 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  | 26.69 ms | 0.116 ms | 0.097 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  | 13.29 ms | 0.040 ms | 0.033 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  | 35.40 ms | 0.240 ms | 0.224 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 46.06 ms | 0.329 ms | 0.308 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  | 44.31 ms | 0.283 ms | 0.265 ms |
```