using System.Text;
using FastPaymentIdemCache.Models;
using FastPaymentIdemCache.Parser;

namespace FastPaymentIdemCache.Tests;

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

    [Fact]
    public void should_decode_set_command_to_constant_and_key()
    {
        var result = CommandParser.Parse(GetSpanBytes("SET user:1 data"));

        var (command, key) = result.Decode();

        Assert.Equal(CommandType.SET, command);
        Assert.Equal("user:1", key);
    }

    [Fact]
    public void should_decode_get_and_delete_commands_to_constants()
    {
        var (getCommand, getKey) = CommandParser.Parse(GetSpanBytes("GET user:1")).Decode();
        var (deleteCommand, deleteKey) = CommandParser.Parse(GetSpanBytes("DELETE user:1")).Decode();

        Assert.Equal(CommandType.GET, getCommand);
        Assert.Equal(CommandType.DELETE, deleteCommand);
        Assert.Equal("user:1", getKey);
        Assert.Equal("user:1", deleteKey);
    }

    [Fact]
    public void should_decode_unknown_command_to_error_constant()
    {
        var (command, key) = CommandParser.Parse(GetSpanBytes("PING user:1")).Decode();

        Assert.Equal(CommandType.ERROR, command);
        Assert.Equal("user:1", key);
    }

    private static Span<byte> GetSpanBytes(string inputStr) => Encoding.UTF8.GetBytes(inputStr).AsSpan();
}