using System.Text.Json;

namespace Wololo.Text.Json;

public readonly record struct JsonResult(JsonTokenType TokenType = JsonTokenType.None, object? Value = null)
{
    /// <summary>
    /// The depth of the token as reported by <see cref="Utf8JsonReader.CurrentDepth"/>: the root value is at depth 0
    /// and the depth increases by one for every enclosing array or object. A <c>StartObject</c>/<c>StartArray</c>
    /// token and its matching <c>EndObject</c>/<c>EndArray</c> token are at the same depth, and property names and
    /// values are at the depth of the object or array that contains them.
    /// </summary>
    public int Depth { get; init; }

    public bool IsValue =>
        TokenType is JsonTokenType.String or JsonTokenType.Number or JsonTokenType.False or JsonTokenType.True;

    /// <summary>
    /// True for a result produced by the <c>rawFromDepth</c> overloads of <see cref="Utf8JsonStreamReader"/>: a
    /// <c>StartObject</c>/<c>StartArray</c> token whose <see cref="Value"/> is the complete raw JSON text of the
    /// object or array (including everything inside it). No tokens are produced for the contents of such a value.
    /// </summary>
    public bool IsRaw => TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray && Value is string;
}
