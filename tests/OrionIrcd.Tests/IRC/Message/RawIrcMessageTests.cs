using OrionIrcd.IRC.Message;

namespace OrionIrcd.Tests.IRC.Message;

public class RawIrcMessageTests
{
    [Fact]
    public void ToString_WhenRawIsProvided_ReturnsRawValue()
    {
        var message = new RawIrcMessage
        {
            Command = "PING",
            Raw = "PING :server"
        };

        var result = message.ToString();

        Assert.Equal("PING :server", result);
    }

    [Fact]
    public void ToString_WithCommandOnly_ReturnsCommandWithoutTrailingSpace()
    {
        var message = new RawIrcMessage { Command = "PING" };

        var result = message.ToString();

        Assert.Equal("PING", result);
    }

    [Fact]
    public void ToString_WithTrailingOnly_ReturnsSingleSeparatorSpace()
    {
        var message = new RawIrcMessage
        {
            Command = "PING",
            Trailing = "server"
        };

        var result = message.ToString();

        Assert.Equal("PING :server", result);
    }

    [Fact]
    public void ToString_WithPrefixParamsAndTrailing_ReturnsIrcLine()
    {
        var message = new RawIrcMessage
        {
            Prefix = new IrcMessagePrefix
            {
                Nick = "nick",
                User = "user",
                Host = "host"
            },
            Command = "PRIVMSG",
            Params = ["#orion"],
            Trailing = "hello world"
        };

        var result = message.ToString();

        Assert.Equal(":nick!user@host PRIVMSG #orion :hello world", result);
    }
}
