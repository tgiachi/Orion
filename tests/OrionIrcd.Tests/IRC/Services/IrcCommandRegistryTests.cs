using OrionIrcd.IRC.Message;
using OrionIrcd.IRC.Services;
using OrionIrcd.Tests.Support.IRC;

namespace OrionIrcd.Tests.IRC.Services;

public class IrcCommandRegistryTests
{
    [Fact]
    public void RegisterCommand_WithRegisteredCommand_CreatesCommandCaseInsensitively()
    {
        var registry = new IrcCommandRegistry();

        registry.RegisterCommand<TestIrcCommand>();

        var result = registry.TryCreate("test", out var command);
        Assert.True(result);
        Assert.IsType<TestIrcCommand>(command);
    }

    [Fact]
    public void RegisterCommand_WithDuplicateCommand_ThrowsInvalidOperationException()
    {
        var registry = new IrcCommandRegistry();
        registry.RegisterCommand<TestIrcCommand>();

        var exception = Assert.Throws<InvalidOperationException>(() => registry.RegisterCommand<TestIrcCommand>());

        Assert.Equal("Command 'TEST' is already registered.", exception.Message);
    }

    [Fact]
    public void RegisterCommand_WithWhitespaceCommandCode_ThrowsInvalidOperationException()
    {
        var registry = new IrcCommandRegistry();

        var exception = Assert.Throws<InvalidOperationException>(() => registry.RegisterCommand<WhitespaceCodeCommand>());

        Assert.Equal("Command code cannot be null or whitespace.", exception.Message);
    }

    [Fact]
    public void TryGetBinder_WithRegisteredBinder_ReturnsTypedBinder()
    {
        var registry = new IrcCommandRegistry();
        registry.RegisterCommand<TestIrcCommand>((command, raw) => command.BoundTrailing = raw.Trailing);
        var command = new TestIrcCommand();
        var rawMessage = new RawIrcMessage
        {
            Command = "TEST",
            Trailing = "bound"
        };

        var result = registry.TryGetBinder(typeof(TestIrcCommand), out var binder);
        binder!(command, rawMessage);

        Assert.True(result);
        Assert.Equal("bound", command.BoundTrailing);
    }
}
