using System.Net;
using OrionIrcd.Network.Interfaces.Client;
using OrionIrcd.Server.Core.Types;

namespace OrionIrcd.Server.Core.Data.Sessions;

public sealed class NetworkSession
{
    public long SessionId { get; }

    public INetworkConnection Connection { get; }

    public EndPoint? RemoteEndPoint { get; }

    public DateTimeOffset ConnectedAtUtc { get; }

    public DateTimeOffset LastActivityAtUtc { get; internal set; }

    public long BytesReceived { get; internal set; }

    public NetworkSessionStatusType Status { get; internal set; }

    public NetworkSession(
        long sessionId,
        INetworkConnection connection,
        EndPoint? remoteEndPoint,
        DateTimeOffset connectedAtUtc
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        SessionId = sessionId;
        Connection = connection;
        RemoteEndPoint = remoteEndPoint;
        ConnectedAtUtc = connectedAtUtc;
        LastActivityAtUtc = connectedAtUtc;
        Status = NetworkSessionStatusType.Connected;
    }
}
