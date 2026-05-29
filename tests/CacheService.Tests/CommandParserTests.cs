using System.Text;

namespace CacheService.Tests;

public class CommandParserTests
{
    [Fact]
    public void should_return_three_arguments_when_parsing_set_command()
    {
        var inputStr = "SET user:1 data";
        var inputStrBytes = GetSpanBytes(inputStr);

        var expected = new CommandParseResult
        {
            Command = GetSpanBytes("SET"),
            Key = GetSpanBytes("user:1"),
            Value = GetSpanBytes("data")
        };

        var actual = CommandParser.Parse(inputStrBytes);

        Assert.True(expected.Equals(actual));
    }

    [Fact]
    public void should_return_two_arguments_when_parsing_get_command()
    {
        var inputStr = "GET user:1";
        var inputStrBytes = GetSpanBytes(inputStr);

        var expected = new CommandParseResult
        {
            Command = GetSpanBytes("GET"),
            Key = GetSpanBytes("user:1"),
            Value = ReadOnlySpan<byte>.Empty
        };

        var actual = CommandParser.Parse(inputStrBytes);

        Assert.True(expected.Equals(actual));
    }

    [Fact]
    public void should_return_empty_when_parsing_command_without_key()
    {
        var inputStr = "GET";
        var inputStrBytes = GetSpanBytes(inputStr);

        var expected = new CommandParseResult
        {
            Command = ReadOnlySpan<byte>.Empty,
            Key = ReadOnlySpan<byte>.Empty,
            Value = ReadOnlySpan<byte>.Empty
        };

        var actual = CommandParser.Parse(inputStrBytes);

        Assert.True(expected.Equals(actual));
    }

    [Fact]
    public void should_return_three_arguments_when_parsing_set_command_with_whitespaces()
    {
        var inputStr = " SET  user:1   data";
        var inputStrBytes = GetSpanBytes(inputStr);

        var expected = new CommandParseResult
        {
            Command = GetSpanBytes("SET"),
            Key = GetSpanBytes("user:1"),
            Value = GetSpanBytes("data")
        };

        var actual = CommandParser.Parse(inputStrBytes);

        Assert.True(expected.Equals(actual));
    }

    [Fact]
    public void should_return_empty_when_parsing_command_without_any()
    {
        var inputStr = "  ";
        var inputStrBytes = GetSpanBytes(inputStr);

        var expected = new CommandParseResult
        {
            Command = ReadOnlySpan<byte>.Empty,
            Key = ReadOnlySpan<byte>.Empty,
            Value = ReadOnlySpan<byte>.Empty
        };

        var actual = CommandParser.Parse(inputStrBytes);

        Assert.True(expected.Equals(actual));
    }

    private static Span<byte> GetSpanBytes(string inputStr) => Encoding.UTF8.GetBytes(inputStr).AsSpan();
}