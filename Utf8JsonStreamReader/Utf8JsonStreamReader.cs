using System.Text.Json;

namespace Wololo.Text.Json;

public sealed class Utf8JsonStreamReader : IDisposable, IAsyncDisposable
{
    private readonly IEnumerator<JsonResult>? enumerator = null;
    private readonly IAsyncEnumerator<JsonResult>? asyncEnumerator = null;

    public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;
    public object? Value { get; private set; }

    public Utf8JsonStreamReader(Stream stream, int bufferSize = -1, bool async = false)
    {
        if (async)
            asyncEnumerator = new Utf8JsonStreamTokenAsyncEnumerable(stream, bufferSize).GetAsyncEnumerator();
        else
            enumerator = new Utf8JsonStreamTokenEnumerable(stream, bufferSize).GetEnumerator();
    }

    public async Task<bool> ReadAsync()
    {
        if (!await asyncEnumerator!.MoveNextAsync())
        {
            TokenType = JsonTokenType.None;
            Value = null;
            return false;
        }
        TokenType = asyncEnumerator.Current.TokenType;
        Value = asyncEnumerator.Current.Value;
        return true;
    }

    public bool Read()
    {
        if (!enumerator!.MoveNext())
        {
            TokenType = JsonTokenType.None;
            Value = null;
            return false;
        }
        TokenType = enumerator.Current.TokenType;
        Value = enumerator.Current.Value;
        return true;
    }

    public void Dispose() => enumerator?.Dispose();
    public ValueTask DisposeAsync() => asyncEnumerator!.DisposeAsync();
}