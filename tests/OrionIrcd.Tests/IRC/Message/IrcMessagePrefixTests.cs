using OrionIrcd.IRC.Message;

namespace OrionIrcd.Tests.IRC.Message;

public class IrcMessagePrefixTests
{
    [Fact]
    public void ToString_WithServerName_ReturnsServerName()
    {
        var prefix = new IrcMessagePrefix { ServerName = "irc.example.net" };

        var result = prefix.ToString();

        Assert.Equal("irc.example.net", result);
    }

    [Fact]
    public void ToString_WithUserPrefix_ReturnsNickUserHost()
    {
        var prefix = new IrcMessagePrefix
        {
            Nick = "nick",
            User = "user",
            Host = "host"
        };

        var result = prefix.ToString();

        Assert.Equal("nick!user@host", result);
    }
}
