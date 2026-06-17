using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Network.Client;

namespace OrionIrcd.Server.Data.Events;

public sealed class NetworkResultReceivedEvent<T> : IEvent
{
    public NetworkResultReceivedEvent(OrionTcpClient client, T result)
    {
        Client = client;
        Result = result;
    }

    public OrionTcpClient Client { get; }

    public T Result { get; }
}
