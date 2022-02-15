# Utf8JsonStreamReader

A streaming JSON parser based on System.Text.Json.Utf8JsonReader.

## Utf8JsonStreamTokenEnumerator

A variant of the above implemented as an enumerator. Is more effective due
to that it can avoid reallocations for token reading within each buffered chunk.

## Performance

```sh
BenchmarkDotNet=v0.13.1, OS=ubuntu 21.10
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.102
  [Host]     : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT


|                                Method | Objects |     Mean |   Error |  StdDev |
|-------------------------------------- |-------- |---------:|--------:|--------:|
|          TraverseUtf8JsonStreamReader |  100000 | 287.9 ms | 3.09 ms | 2.58 ms |
| TraverseUtf8JsonStreamTokenEnumerator |  100000 | 267.1 ms | 4.25 ms | 3.97 ms |
|      TraverseNewtonsoftJsonTextReader |  100000 | 286.4 ms | 2.95 ms | 2.47 ms |
```
