# Utf8JsonStreamReader

A streaming JSON parser based on System.Text.Json.Utf8JsonReader.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

```sh
BenchmarkDotNet v0.13.12, Ubuntu 23.10 (Mantic Minotaur)
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.103
  [Host]     : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                                     | Objects | Mean     | Error   | StdDev  |
|------------------------------------------- |-------- |---------:|--------:|--------:|
| TraverseUtf8JsonStreamReaderAsync          | 100000  | 138.9 ms | 0.61 ms | 0.48 ms |
| TraverseUtf8JsonStreamReader               | 100000  | 101.8 ms | 0.36 ms | 0.30 ms |
| TraverseUtf8JsonStreamTokenAsyncEnumerable | 100000  | 139.0 ms | 2.66 ms | 3.56 ms |
| TraverseUtf8JsonStreamTokenEnumerable      | 100000  | 100.1 ms | 0.45 ms | 0.40 ms |
| TraverseNewtonsoftJsonTextReaderAsync      | 100000  | 185.4 ms | 0.75 ms | 0.62 ms |
| TraverseNewtonsoftJsonTextReader           | 100000  | 110.9 ms | 0.62 ms | 0.58 ms |
```
