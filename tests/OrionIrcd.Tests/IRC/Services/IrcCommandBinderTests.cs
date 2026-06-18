using OrionIrcd.IRC.Commands.Internal;
using OrionIrcd.IRC.Message;
using OrionIrcd.IRC.Services;
using OrionIrcd.Tests.Support.IRC;

namespace OrionIrcd.Tests.IRC.Services;

public class IrcCommandBinderTests
{
    [Fact]
    public void Bind_WithBaseCommand_CopiesRawFieldsAndInvokesTypedBinder()
    {
        var registry = new IrcCommandRegistry();
        registry.RegisterCommand<TestBaseCommand>((command, raw) => command.BoundTrailing = raw.Trailing);
        var binder = new IrcCommandBinder(registry);
        var command = new TestBaseCommand();
        var prefix = new IrcMessagePrefix
        {
            Nick = "nick",
            User = "user",
            Host = "host"
        };
        var tags = new Dictionary<string, string?> { ["tag"] = "value" };
        var rawMessage = new RawIrcMessage
        {
            Command = "TEST",
            Raw = ":nick!user@host TEST #orion :hello",
            Prefix = prefix,
            Tags = tags,
            Params = ["#orion"],
            Trailing = "hello"
        };

        binder.Bind(command, rawMessage);

        Assert.Equal(rawMessage.Raw, command.Raw);
        Assert.Same(prefix, command.Prefix);
        Assert.Same(tags, command.Tags);
        Assert.Equal(["#orion"], command.Params);
        Assert.Equal("hello", command.Trailing);
        Assert.Equal("hello", command.BoundTrailing);
    }

    [Fact]
    public void Bind_WithNotParsedCommand_CopiesRawFieldsAndUsesRawAsMessage()
    {
        var registry = new IrcCommandRegistry();
        var binder = new IrcCommandBinder(registry);
        var command = new NotParsedCommand();
        var rawMessage = new RawIrcMessage
        {
            Command = "UNKNOWN",
            Raw = "UNKNOWN :payload",
            Params = [],
            Trailing = "payload"
        };

        binder.Bind(command, rawMessage);

        Assert.Equal("UNKNOWN", command.Code);
        Assert.Equal("UNKNOWN :payload", command.Raw);
        Assert.Equal("UNKNOWN :payload", command.Message);
        Assert.Equal("payload", command.Trailing);
    }
}
