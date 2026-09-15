using System.Text;

namespace FastPaymentIdemCache.Models;

public readonly ref struct CommandParseResult
{
    public required ReadOnlySpan<byte> Command { get; init; }

    public required ReadOnlySpan<byte> Key { get; init; }

    public ReadOnlySpan<byte> Value { get; init; }

    public static CommandParseResult Empty =>
        new()
        {
            Command = ReadOnlySpan<byte>.Empty,
            Key = ReadOnlySpan<byte>.Empty,
            Value = ReadOnlySpan<byte>.Empty
        };

    public (string command, string key, byte[]? value) Decode()
    {
        var command = Command.IsEmpty ? string.Empty : Encoding.UTF8.GetString(Command);
        var key = Key.IsEmpty ? string.Empty : Encoding.UTF8.GetString(Key);
        var value = Value.IsEmpty ? null : Value.ToArray();

        return (command, key, value);
    }

    public bool Equals(CommandParseResult other) =>
        Command.SequenceEqual(other.Command) &&
        Key.SequenceEqual(other.Key) &&
        Value.SequenceEqual(other.Value);
}