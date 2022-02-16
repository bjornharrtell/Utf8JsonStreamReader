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

|                                Method | Objects |     Mean |   Error |  StdDev |
|-------------------------------------- |-------- |---------:|--------:|--------:|
|          TraverseUtf8JsonStreamReader |  100000 | 195.9 ms | 1.46 ms | 2.19 ms |
| TraverseUtf8JsonStreamTokenEnumerator |  100000 | 172.0 ms | 1.35 ms | 2.01 ms |
|      TraverseNewtonsoftJsonTextReader |  100000 | 257.2 ms | 3.17 ms | 4.74 ms |
```
