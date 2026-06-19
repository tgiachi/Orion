using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Network.Interfaces.Client;

namespace OrionIrcd.Server.Core.Data.Events;

public sealed class NetworkResultReceivedEvent<T> : IEvent
{
    public NetworkResultReceivedEvent(INetworkConnection connection, T result)
    {
        Connection = connection;
        Result = result;
    }

    public INetworkConnection Connection { get; }

    public T Result { get; }
}
