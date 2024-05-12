# Utf8JsonStreamReader

A streaming JSON parser based on `System.Text.Json.Utf8JsonReader`.

## Performance

Results produced by:

> sudo dotnet run -c Release --project Benchmarks

```sh
BenchmarkDotNet v0.13.12, Ubuntu 24.04 LTS (Noble Numbat)
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.104
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                                        | Objects | Mean      | Error    | StdDev   |
|---------------------------------------------- |-------- |----------:|---------:|---------:|
| TraverseUtf8JsonStreamReader                  | 100000  |  55.22 ms | 0.655 ms | 0.613 ms |
| TraverseUtf8JsonStreamReaderAsync             | 100000  |  55.93 ms | 0.534 ms | 0.473 ms |
| TraverseUtf8JsonStreamReaderRawValue          | 100000  |  25.07 ms | 0.359 ms | 0.336 ms |
| TraverseUtf8JsonStreamReaderToEnumerable      | 100000  |  70.22 ms | 0.857 ms | 0.802 ms |
| TraverseUtf8JsonStreamReaderToAsyncEnumerable | 100000  | 105.68 ms | 0.882 ms | 0.782 ms |
| TraverseNewtonsoftJsonTextReader              | 100000  |  91.23 ms | 1.005 ms | 0.940 ms |
```
