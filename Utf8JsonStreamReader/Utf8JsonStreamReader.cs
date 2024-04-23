using System.Text.Json;

namespace Wololo.Text.Json;

public static class Utf8JsonStreamReader
{
    public delegate void OnRead(ref Utf8JsonReader reader);

    public static void Read(Stream stream, OnRead action, int bufferSize = -1)
    {
        bufferSize = bufferSize <= 0 ? 1024 * 8 : bufferSize;
        bool done = false;
        Memory<byte> buffer = new byte[bufferSize];
        int bufferLength = 0;
        int offset = 0;
        JsonReaderState jsonReaderState = new();
        while (!done)
        {
            var remaining = bufferLength - offset;
            if (remaining > 0)
                buffer[offset..].CopyTo(buffer);
            var readLength = stream.ReadAtLeast(buffer[remaining..].Span, bufferSize - remaining, false);
            bufferLength = readLength + remaining;
            offset = 0;
            done = bufferLength < bufferSize;
            var reader = new Utf8JsonReader(buffer[offset..bufferLength].Span, done, jsonReaderState);
            while (reader.Read())
                action(ref reader);
            jsonReaderState = reader.CurrentState;
            offset = (int)reader.BytesConsumed;
        }
    }
}