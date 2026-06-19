using OrionIrcd.IRC.Commands.Base;

namespace OrionIrcd.Tests.IRC.Commands.Base;

public class BaseIrcCommandProducerTests
{
    [Fact]
    public void CapCommand_Produce_ReturnsRawMessage()
    {
        var command = new CapCommand
        {
            Subcommand = "LS",
            Capabilities = ["302"]
        };

        var raw = command.Produce();

        Assert.Equal("CAP", raw.Command);
        Assert.Equal(["LS", "302"], raw.Params);
        Assert.Null(raw.Trailing);
        Assert.Equal("CAP LS 302", raw.ToString());
    }

    [Fact]
    public void MotdCommand_Produce_ReturnsRawMessage()
    {
        var raw = new MotdCommand().Produce();

        Assert.Equal("MOTD", raw.Command);
        Assert.Empty(raw.Params);
        Assert.Null(raw.Trailing);
        Assert.Equal("MOTD", raw.ToString());
    }

    [Fact]
    public void NickCommand_Produce_ReturnsRawMessage()
    {
        var command = new NickCommand { Nickname = "squid" };

        var raw = command.Produce();

        Assert.Equal("NICK", raw.Command);
        Assert.Equal(["squid"], raw.Params);
        Assert.Null(raw.Trailing);
        Assert.Equal("NICK squid", raw.ToString());
    }

    [Fact]
    public void PassCommand_Produce_ReturnsRawMessage()
    {
        var command = new PassCommand { Password = "server-secret" };

        var raw = command.Produce();

        Assert.Equal("PASS", raw.Command);
        Assert.Equal(["server-secret"], raw.Params);
        Assert.Null(raw.Trailing);
        Assert.Equal("PASS server-secret", raw.ToString());
    }

    [Fact]
    public void PingCommand_Produce_ReturnsRawMessage()
    {
        var command = new PingCommand { Token = "abc123" };

        var raw = command.Produce();

        Assert.Equal("PING", raw.Command);
        Assert.Empty(raw.Params);
        Assert.Equal("abc123", raw.Trailing);
        Assert.Equal("PING :abc123", raw.ToString());
    }

    [Fact]
    public void PongCommand_Produce_ReturnsRawMessage()
    {
        var command = new PongCommand { Token = "abc123" };

        var raw = command.Produce();

        Assert.Equal("PONG", raw.Command);
        Assert.Empty(raw.Params);
        Assert.Equal("abc123", raw.Trailing);
        Assert.Equal("PONG :abc123", raw.ToString());
    }

    [Fact]
    public void QuitCommand_Produce_ReturnsRawMessage()
    {
        var command = new QuitCommand { Reason = "Client Quit" };

        var raw = command.Produce();

        Assert.Equal("QUIT", raw.Command);
        Assert.Empty(raw.Params);
        Assert.Equal("Client Quit", raw.Trailing);
        Assert.Equal("QUIT :Client Quit", raw.ToString());
    }

    [Fact]
    public void UserCommand_Produce_ReturnsRawMessage()
    {
        var command = new UserCommand
        {
            Username = "squiduser",
            Mode = "0",
            Unused = "*",
            RealName = "Squid User"
        };

        var raw = command.Produce();

        Assert.Equal("USER", raw.Command);
        Assert.Equal(["squiduser", "0", "*"], raw.Params);
        Assert.Equal("Squid User", raw.Trailing);
        Assert.Equal("USER squiduser 0 * :Squid User", raw.ToString());
    }
}
