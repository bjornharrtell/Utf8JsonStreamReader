# Utf8JsonStreamReader

A streaming JSON parser based on System.Text.Json.Utf8JsonReader.

## Utf8JsonStreamTokenEnumerator

A variant of the above implemented as an enumerator. Is more effective due
to that it can avoid reallocations for token reading within each buffered chunk.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

### 0.16.x (.NET Standard 2.0 / .NET 6)

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

### 0.17.x (.NET 8)

```sh
BenchmarkDotNet v0.13.10, Ubuntu 23.10 (Mantic Minotaur)
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100-rc.2.23502.1
  [Host]     : .NET 8.0.0 (8.0.23.47517), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.47517), X64 RyuJIT AVX2


| Method                                | Objects | Mean     | Error   | StdDev  |
|-------------------------------------- |-------- |---------:|--------:|--------:|
| TraverseUtf8JsonStreamReader          | 100000  | 149.5 ms | 1.70 ms | 1.59 ms |
| TraverseUtf8JsonStreamTokenEnumerator | 100000  | 131.8 ms | 2.63 ms | 3.78 ms |
| TraverseNewtonsoftJsonTextReader      | 100000  | 160.3 ms | 1.11 ms | 0.98 ms
```
