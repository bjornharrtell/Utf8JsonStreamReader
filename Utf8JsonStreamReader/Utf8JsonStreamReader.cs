using System.Text.Json;

namespace Wololo.Text.Json;

public sealed class Utf8JsonStreamReader : IDisposable, IAsyncDisposable
{
    private IEnumerator<JsonResult>? enumerator = null;
    private IAsyncEnumerator<JsonResult>? asyncEnumerator = null;

    public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;
    public object? Value { get; private set; }

    public Utf8JsonStreamReader(Stream stream, int bufferSize = -1, bool async = false)
    {
        if (async)
            this.asyncEnumerator = new Utf8JsonStreamTokenAsyncEnumerable(stream, bufferSize).GetAsyncEnumerator();
        else
            this.enumerator = new Utf8JsonStreamTokenEnumerable(stream, bufferSize).GetEnumerator();
    }

    public async Task<bool> ReadAsync()
    {
        if (!await asyncEnumerator!.MoveNextAsync()) {
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
        if (!enumerator!.MoveNext()) {
            TokenType = JsonTokenType.None;
            Value = null;
            return false;
        }
        TokenType = enumerator.Current.TokenType;
        Value = enumerator.Current.Value;
        return true;
    }

    public void Dispose()
    {
        enumerator?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return asyncEnumerator!.DisposeAsync();
    }
}