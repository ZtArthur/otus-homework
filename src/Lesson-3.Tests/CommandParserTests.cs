namespace Lesson_3.Tests;

public class CommandParserTests
{
    [Fact]
    public void should_return_three_arguments_when_parsing_set_command()
    {
        var command = "SET user:1 data";
        var expected = new CommandParseResult
        {
            Command = "SET",
            Key = "user:1",
            Value = "data"
        };

        var actual = CommandParser.Parse(command);

        Assert.True(expected.Equals(actual));
    }

    [Fact]
    public void should_return_two_arguments_when_parsing_get_command()
    {
        var command = "GET user:1";
        var expected = new CommandParseResult
        {
            Command = "GET",
            Key = "user:1",
            Value = ReadOnlySpan<char>.Empty
        };

        var actual = CommandParser.Parse(command);

        Assert.True(expected.Equals(actual));
    }
    
    [Fact]
    public void should_return_empty_when_parsing_command_without_key()
    {
        var command = "GET";
        var expected = CommandParseResult.Empty;

        var actual = CommandParser.Parse(command);

        Assert.True(expected.Equals(actual));
    }
    
    [Fact]
    public void should_return_three_arguments_when_parsing_set_command_with_whitespaces()
    {
        var command = " SET  user:1   data";
        var expected = new CommandParseResult
        {
            Command = "SET",
            Key = "user:1",
            Value = "data"
        };

        var actual = CommandParser.Parse(command);

        Assert.True(expected.Equals(actual));
    }
    
    [Fact]
    public void should_return_empty_when_parsing_command_without_any()
    {
        var command = "  ";
        var expected = CommandParseResult.Empty;

        var actual = CommandParser.Parse(command);

        Assert.True(expected.Equals(actual));
    }
}