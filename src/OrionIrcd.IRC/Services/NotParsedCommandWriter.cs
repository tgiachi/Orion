using OrionIrcd.IRC.Commands.Internal;
using OrionIrcd.IRC.Data;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Types;

namespace OrionIrcd.IRC.Services;

public sealed class NotParsedCommandWriter : IIrcCommandWriter<NotParsedCommand>
{
    public IrcCommandResult<string> Write(NotParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return IrcCommandResult<string>.Failure(
                new()
                {
                    Type = IrcCommandErrorType.Validation,
                    Message = "Command code is required."
                });
        }

        var output = string.IsNullOrWhiteSpace(command.Message)
                         ? command.Code
                         : $"{command.Code} {command.Message}";

        return IrcCommandResult<string>.Success(output);
    }
}
