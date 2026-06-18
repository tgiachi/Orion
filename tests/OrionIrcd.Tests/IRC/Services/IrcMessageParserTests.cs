using System.Text;
using OrionIrcd.IRC.Services;

namespace OrionIrcd.Tests.IRC.Services;

public class IrcMessageParserTests
{
    [Fact]
    public void ParseMessages_CompletePrivmsgWithUserPrefix_ReturnsStructuredMessage()
    {
        var parser = new IrcMessageParser();

        var messages = parser.ParseMessages(ToBytes(":nick!user@host PRIVMSG #orion :hello world\r\n"));

        var message = Assert.Single(messages);
        Assert.Equal("PRIVMSG", message.Command);
        Assert.Equal(["#orion"], message.Params);
        Assert.Equal("hello world", message.Trailing);
        Assert.Equal(":nick!user@host PRIVMSG #orion :hello world", message.Raw);
        Assert.NotNull(message.Prefix);
        Assert.True(message.Prefix!.IsUser);
        Assert.False(message.Prefix.IsServer);
        Assert.Equal("nick", message.Prefix.Nick);
        Assert.Equal("user", message.Prefix.User);
        Assert.Equal("host", message.Prefix.Host);
    }

    [Fact]
    public void ParseMessages_CustomSeparator_UsesProvidedSeparator()
    {
        var parser = new IrcMessageParser();

        var messages = parser.ParseMessages(ToBytes("PING :one\nPONG :two\n"), "\n");

        Assert.Equal(2, messages.Count);
        Assert.Equal("one", messages[0].Trailing);
        Assert.Equal("two", messages[1].Trailing);
    }

    [Fact]
    public void ParseMessages_IrcV3Tags_UnescapesValuesAndKeepsValuelessTag()
    {
        var parser = new IrcMessageParser();

        var messages = parser.ParseMessages(
            ToBytes("@custom=hello\\sworld\\:semi\\\\slash\\r\\nend;client-only PING :server\r\n")
        );

        var message = Assert.Single(messages);
        Assert.Equal("PING", message.Command);
        Assert.True(message.Tags.ContainsKey("client-only"));
        Assert.Null(message.Tags["client-only"]);
        Assert.Equal("hello world;semi\\slash\r\nend", message.Tags["custom"]);
    }

    [Fact]
    public void ParseMessages_MultipleMessagesInOneBuffer_ReturnsAllMessages()
    {
        var parser = new IrcMessageParser();

        var messages = parser.ParseMessages(ToBytes("PING :one\r\nPONG server :two\r\n"));

        Assert.Equal(2, messages.Count);
        Assert.Equal("PING", messages[0].Command);
        Assert.Equal("one", messages[0].Trailing);
        Assert.Equal("PONG", messages[1].Command);
        Assert.Equal(["server"], messages[1].Params);
        Assert.Equal("two", messages[1].Trailing);
    }

    [Fact]
    public void ParseMessages_PartialMessageAcrossCalls_BuffersUntilSeparator()
    {
        var parser = new IrcMessageParser();

        var first = parser.ParseMessages(ToBytes("PRIVMSG #orion :hel"));
        var second = parser.ParseMessages(ToBytes("lo\r\n"));

        Assert.Empty(first);
        var message = Assert.Single(second);
        Assert.Equal("PRIVMSG", message.Command);
        Assert.Equal(["#orion"], message.Params);
        Assert.Equal("hello", message.Trailing);
    }

    [Fact]
    public void ParseMessages_ServerPrefix_ReturnsServerName()
    {
        var parser = new IrcMessageParser();

        var messages = parser.ParseMessages(ToBytes(":irc.example.net PING :orion\r\n"));

        var message = Assert.Single(messages);
        Assert.Equal("PING", message.Command);
        Assert.Equal("orion", message.Trailing);
        Assert.NotNull(message.Prefix);
        Assert.True(message.Prefix!.IsServer);
        Assert.False(message.Prefix.IsUser);
        Assert.Equal("irc.example.net", message.Prefix.ServerName);
    }

    [Fact]
    public void ParseMessages_WhenBufferWouldOverflow_ThrowsInvalidOperationException()
    {
        var parser = new IrcMessageParser();
        var oversized = new byte[65537];

        var exception = Assert.Throws<InvalidOperationException>(() => parser.ParseMessages(oversized));

        Assert.Equal("Parser buffer overflow. Message too large.", exception.Message);
    }

    private static byte[] ToBytes(string value)
        => Encoding.UTF8.GetBytes(value);
}
