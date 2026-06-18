using System.Net;
using OrionIrcd.Network.Interfaces.Client;

namespace OrionIrcd.Tests.Support.Network;

public sealed class TestNetworkConnection : INetworkConnection
{
    public long SessionId { get; init; } = 1;

    public EndPoint? RemoteEndPoint { get; init; } = new IPEndPoint(IPAddress.Loopback, 6667);

    public bool IsConnected { get; set; } = true;

    public int CloseCallCount { get; private set; }

    public List<byte[]> SentPayloads { get; } = [];

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CloseCallCount++;
        IsConnected = false;

        return Task.CompletedTask;
    }

    public Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SentPayloads.Add(payload.ToArray());

        return Task.CompletedTask;
    }
}
