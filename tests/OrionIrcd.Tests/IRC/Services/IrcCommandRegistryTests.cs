using OrionIrcd.IRC.Services;
using OrionIrcd.Tests.Support.IRC;

namespace OrionIrcd.Tests.IRC.Services;

public class IrcCommandRegistryTests
{
    [Fact]
    public void RegisterCommand_WithDuplicateCommand_ThrowsInvalidOperationException()
    {
        var registry = new IrcCommandRegistry();
        registry.RegisterCommand<TestIrcCommand>();

        var exception = Assert.Throws<InvalidOperationException>(() => registry.RegisterCommand<TestIrcCommand>());

        Assert.Equal("Command 'TEST' is already registered.", exception.Message);
    }

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
    public void RegisterCommand_WithWhitespaceCommandCode_ThrowsInvalidOperationException()
    {
        var registry = new IrcCommandRegistry();

        var exception = Assert.Throws<InvalidOperationException>(() => registry.RegisterCommand<WhitespaceCodeCommand>());

        Assert.Equal("Command code cannot be null or whitespace.", exception.Message);
    }

    [Fact]
    public void TryCreate_WithUnknownCommand_ReturnsFalse()
    {
        var registry = new IrcCommandRegistry();

        var result = registry.TryCreate("UNKNOWN", out var command);

        Assert.False(result);
        Assert.Null(command);
    }
}
