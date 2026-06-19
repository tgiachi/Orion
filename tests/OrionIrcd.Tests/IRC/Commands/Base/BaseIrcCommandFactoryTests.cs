using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Message;
using OrionIrcd.IRC.Services;

namespace OrionIrcd.Tests.IRC.Commands.Base;

public class BaseIrcCommandFactoryTests
{
    [Fact]
    public void CreateOrFallback_WithCapMessage_ReturnsBoundCapCommand()
    {
        var factory = CreateFactory();
        var raw = new RawIrcMessage
        {
            Command = "CAP",
            Params = ["LS", "302"],
            Raw = "CAP LS 302"
        };

        var command = Assert.IsType<CapCommand>(factory.CreateOrFallback(raw));

        Assert.Equal("LS", command.Subcommand);
        Assert.Equal(["302"], command.Capabilities);
    }

    [Fact]
    public void CreateOrFallback_WithNickMessage_ReturnsBoundNickCommand()
    {
        var factory = CreateFactory();
        var raw = new RawIrcMessage
        {
            Command = "NICK",
            Params = ["squid"],
            Raw = "NICK squid"
        };

        var command = Assert.IsType<NickCommand>(factory.CreateOrFallback(raw));

        Assert.Equal("NICK", command.Code);
        Assert.Equal("squid", command.Nickname);
        Assert.Equal("NICK squid", command.Raw);
    }

    [Fact]
    public void CreateOrFallback_WithPingMessage_ReturnsBoundPingCommand()
    {
        var factory = CreateFactory();
        var raw = new RawIrcMessage
        {
            Command = "PING",
            Trailing = "abc123",
            Raw = "PING :abc123"
        };

        var command = Assert.IsType<PingCommand>(factory.CreateOrFallback(raw));

        Assert.Equal("abc123", command.Token);
    }

    [Fact]
    public void CreateOrFallback_WithPassMessage_ReturnsBoundPassCommand()
    {
        var factory = CreateFactory();
        var raw = new RawIrcMessage
        {
            Command = "PASS",
            Params = ["server-secret"],
            Raw = "PASS server-secret"
        };

        var command = Assert.IsType<PassCommand>(factory.CreateOrFallback(raw));

        Assert.Equal("PASS", command.Code);
        Assert.Equal("server-secret", command.Password);
        Assert.Equal("PASS server-secret", command.Raw);
    }

    [Fact]
    public void CreateOrFallback_WithMotdMessage_ReturnsMotdCommand()
    {
        var factory = CreateFactory();
        var raw = new RawIrcMessage
        {
            Command = "MOTD",
            Raw = "MOTD"
        };

        var command = Assert.IsType<MotdCommand>(factory.CreateOrFallback(raw));

        Assert.Equal("MOTD", command.Code);
        Assert.Equal("MOTD", command.Raw);
    }

    [Fact]
    public void CreateOrFallback_WithUserMessage_ReturnsBoundUserCommand()
    {
        var factory = CreateFactory();
        var raw = new RawIrcMessage
        {
            Command = "USER",
            Params = ["squid", "0", "*"],
            Trailing = "Squid User",
            Raw = "USER squid 0 * :Squid User"
        };

        var command = Assert.IsType<UserCommand>(factory.CreateOrFallback(raw));

        Assert.Equal("squid", command.Username);
        Assert.Equal("0", command.Mode);
        Assert.Equal("*", command.Unused);
        Assert.Equal("Squid User", command.RealName);
    }

    private static IrcCommandFactory CreateFactory()
    {
        var registry = new IrcCommandRegistry();
        RegisterCommands(registry);

        return new(registry, new());
    }

    private static void RegisterCommands(IrcCommandRegistry registry)
    {
        registry.RegisterCommand<CapCommand>();
        registry.RegisterCommand<MotdCommand>();
        registry.RegisterCommand<NickCommand>();
        registry.RegisterCommand<PassCommand>();
        registry.RegisterCommand<PingCommand>();
        registry.RegisterCommand<PongCommand>();
        registry.RegisterCommand<QuitCommand>();
        registry.RegisterCommand<UserCommand>();
    }
}
