using OrionIrcd.IRC.Commands.Internal;
using OrionIrcd.IRC.Services;
using OrionIrcd.IRC.Types;

namespace OrionIrcd.Tests.IRC.Services;

public class NotParsedCommandWriterTests
{
    [Fact]
    public void Write_WithoutCode_ReturnsValidationFailure()
    {
        var writer = new NotParsedCommandWriter();
        var command = new NotParsedCommand();

        var result = writer.Write(command);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(IrcCommandErrorType.Validation, result.Error!.Type);
        Assert.Equal("Command code is required.", result.Error.Message);
    }

    [Fact]
    public void Write_WithCommandOnly_ReturnsCommandCode()
    {
        var writer = new NotParsedCommandWriter();
        var command = new NotParsedCommand { Code = "PING" };

        var result = writer.Write(command);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal("PING", result.Value);
    }

    [Fact]
    public void Write_WithMessage_ReturnsCommandLine()
    {
        var writer = new NotParsedCommandWriter();
        var command = new NotParsedCommand
        {
            Code = "UNKNOWN",
            Message = "one two"
        };

        var result = writer.Write(command);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal("UNKNOWN one two", result.Value);
    }
}
