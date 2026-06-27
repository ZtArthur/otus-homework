using CacheService.Models;

namespace CacheService.Parser;

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