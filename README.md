# Utf8JsonStreamReader

A streaming JSON parser based on System.Text.Json.Utf8JsonReader.

## Utf8JsonStreamTokenEnumerator

A variant of the above implemented as an enumerator. Is more effective due
to that it can avoid reallocations for token reading within each buffered chunk.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmark

```sh
BenchmarkDotNet=v0.13.2, OS=ubuntu 22.10
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.403
  [Host]     : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT AVX2


|                                Method | Objects |     Mean |   Error |  StdDev |
|-------------------------------------- |-------- |---------:|--------:|--------:|
|          TraverseUtf8JsonStreamReader |  100000 | 228.2 ms | 1.87 ms | 1.56 ms |
| TraverseUtf8JsonStreamTokenEnumerator |  100000 | 186.6 ms | 1.73 ms | 1.62 ms |
|      TraverseNewtonsoftJsonTextReader |  100000 | 255.0 ms | 3.44 ms | 2.87 ms |

```
