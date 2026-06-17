using System.Text;
using OrionIrcd.Network.Client;
using OrionIrcd.Network.Interfaces.Processing;

namespace OrionIrcd.Server.Services.Network;

public sealed class StringProcessor : IResultProcessor<string>
{
    public ValueTask<string> ProcessAsync(
        OrionTcpClient client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (data.IsEmpty)
        {
            return ValueTask.FromResult(string.Empty);
        }

        var command = Encoding.UTF8.GetString(data.Span);

        if (command.EndsWith("\r\n", StringComparison.Ordinal))
        {
            command = command[..^2];
        }
        else if (command.EndsWith('\n'))
        {
            command = command[..^1];
        }

        return ValueTask.FromResult(command);
    }
}
