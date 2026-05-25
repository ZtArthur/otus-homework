namespace Lesson_3;

public static class CommandParser
{
    public static CommandParseResult Parse(ReadOnlySpan<char> input)
    {
        input = input.Trim();

        if (input.IsEmpty)
        {
            return CommandParseResult.Empty;
        }

        var part1 = input.IndexOf(value: ' ');

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

        var part2 = input.IndexOf(value: ' ');

        var key = part2 < 0
            ? input
            : input[..part2];

        if (part2 < 0)
        {
            return new CommandParseResult
            {
                Command = command,
                Key = key,
                Value = ReadOnlySpan<char>.Empty
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

    private static ReadOnlySpan<char> SkipSpaces(ReadOnlySpan<char> s)
    {
        var i = 0;

        while (i < s.Length && s[i] == ' ')
        {
            i++;
        }

        return s[i..];
    }
}

public readonly ref struct CommandParseResult
{
    public required ReadOnlySpan<char> Command { get; init; }

    public required ReadOnlySpan<char> Key { get; init; }

    public ReadOnlySpan<char> Value { get; init; }

    public static CommandParseResult Empty =>
        new()
        {
            Command = ReadOnlySpan<char>.Empty,
            Key = ReadOnlySpan<char>.Empty,
            Value = ReadOnlySpan<char>.Empty
        };

    public bool Equals(CommandParseResult other) =>
        Command.SequenceEqual(other.Command) &&
        Key.SequenceEqual(other.Key) &&
        Value.SequenceEqual(other.Value);
}