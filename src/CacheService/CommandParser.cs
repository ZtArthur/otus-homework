using System.Text;

namespace CacheService;

public static class CommandParser
{
    private const byte _delimiter = (byte)' ';

    public static CommandParseResult Parse(ReadOnlySpan<byte> input)
    {
        input = SkipSpaces(input);

        if (input.Length == 0)
        {
            return CommandParseResult.Empty;
        }

        var part1 = input.IndexOf(_delimiter);

        if (part1 < 0)
        {
            return CommandParseResult.Empty;
        }

        var command = input[..part1];

        input = SkipSpaces(input[(part1 + 1)..]);

        if (input.IsEmpty)
        {
            return CommandParseResult.Empty;
        }

        var part2 = input.IndexOf(_delimiter);

        var key = part2 < 0
            ? input
            : input[..part2];

        if (part2 < 0)
        {
            return new CommandParseResult
            {
                Command = command,
                Key = key,
                Value = ReadOnlySpan<byte>.Empty
            };
        }

        var value = SkipSpaces(input[(part2 + 1)..]);

        return new CommandParseResult
        {
            Command = command,
            Key = key,
            Value = value
        };
    }

    private static ReadOnlySpan<byte> SkipSpaces(ReadOnlySpan<byte> s)
    {
        var i = 0;

        while (i < s.Length && s[i] == _delimiter)
        {
            i++;
        }

        return s[i..];
    }
}

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

    public string AsString()
    {
        var command = Command.IsEmpty ? string.Empty : Encoding.UTF8.GetString(Command);
        var key = Encoding.UTF8.GetString(Key);

        if (Value.IsEmpty)
        {
            return $"{command} {key}";
        }

        var value = Encoding.UTF8.GetString(Value);
        return $"{command} {key} {value}";
    }

    public bool Equals(CommandParseResult other) =>
        Command.SequenceEqual(other.Command) &&
        Key.SequenceEqual(other.Key) &&
        Value.SequenceEqual(other.Value);
}