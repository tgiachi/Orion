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

        return new(registry, new(registry));
    }

    private static void RegisterCommands(IrcCommandRegistry registry)
    {
        registry.RegisterCommand<CapCommand>(
            (command, raw) =>
            {
                command.Subcommand = raw.Params.Count > 0 ? raw.Params[0] : string.Empty;
                command.Capabilities = raw.Params.Skip(1).ToArray();
            }
        );
        registry.RegisterCommand<NickCommand>(
            (command, raw) => command.Nickname = raw.Params.Count > 0 ? raw.Params[0] : string.Empty
        );
        registry.RegisterCommand<PingCommand>(
            (command, raw) => command.Token = raw.Trailing ?? (raw.Params.Count > 0 ? raw.Params[0] : string.Empty)
        );
        registry.RegisterCommand<PongCommand>(
            (command, raw) => command.Token = raw.Trailing ?? (raw.Params.Count > 0 ? raw.Params[0] : string.Empty)
        );
        registry.RegisterCommand<QuitCommand>(
            (command, raw) => command.Reason = raw.Trailing ?? string.Empty
        );
        registry.RegisterCommand<UserCommand>(
            (command, raw) =>
            {
                command.Username = raw.Params.Count > 0 ? raw.Params[0] : string.Empty;
                command.Mode = raw.Params.Count > 1 ? raw.Params[1] : string.Empty;
                command.Unused = raw.Params.Count > 2 ? raw.Params[2] : string.Empty;
                command.RealName = raw.Trailing ?? string.Empty;
            }
        );
    }
}
