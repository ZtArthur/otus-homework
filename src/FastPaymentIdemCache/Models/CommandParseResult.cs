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

    public (string command, string key) Decode()
    {
        var command = DecodeCommand();
        var key = Key.IsEmpty ? string.Empty : Encoding.UTF8.GetString(Key);

        return (command, key);
    }

    private string DecodeCommand()
    {
        if (Command.SequenceEqual("SET"u8))
        {
            return CommandType.SET;
        }

        if (Command.SequenceEqual("GET"u8))
        {
            return CommandType.GET;
        }

        if (Command.SequenceEqual("DELETE"u8))
        {
            return CommandType.DELETE;
        }

        return CommandType.ERROR;
    }

    public bool Equals(CommandParseResult other) =>
        Command.SequenceEqual(other.Command) &&
        Key.SequenceEqual(other.Key) &&
        Value.SequenceEqual(other.Value);
}