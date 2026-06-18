using System.Collections.Concurrent;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Network.Interfaces.Client;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Interfaces.Services;
using OrionIrcd.Server.Types;
using Serilog;

namespace OrionIrcd.Server.Services.Sessions;

public sealed class SessionManagerService : ISessionManagerService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger = Log.ForContext<SessionManagerService>();
    private readonly ConcurrentDictionary<long, NetworkSession> _sessions = new();
    private readonly TimeProvider _timeProvider;

    public SessionManagerService(IEventBus eventBus, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        _eventBus = eventBus;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var session in GetSessions())
        {
            await CloseAsync(session.SessionId, cancellationToken).ConfigureAwait(false);
            Unregister(session.Connection);
        }
    }

    public NetworkSession Register(INetworkConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (_sessions.TryGetValue(connection.SessionId, out var existingSession))
        {
            return existingSession;
        }

        var now = _timeProvider.GetUtcNow();
        var session = new NetworkSession(
            connection.SessionId,
            connection,
            connection.RemoteEndPoint,
            now
        );

        if (!_sessions.TryAdd(connection.SessionId, session))
        {
            return _sessions[connection.SessionId];
        }

        _eventBus.Publish(new NetworkSessionConnectedEvent(session));

        return session;
    }

    public NetworkSession? Unregister(INetworkConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (!_sessions.TryRemove(connection.SessionId, out var session))
        {
            return null;
        }

        session.Status = NetworkSessionStatusType.Disconnected;
        session.LastActivityAtUtc = _timeProvider.GetUtcNow();
        _eventBus.Publish(new NetworkSessionDisconnectedEvent(session));

        return session;
    }

    public NetworkSession RecordActivity(INetworkConnection connection, ReadOnlyMemory<byte> data)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var session = Register(connection);
        session.LastActivityAtUtc = _timeProvider.GetUtcNow();
        session.BytesReceived += data.Length;
        _eventBus.Publish(new NetworkSessionDataReceivedEvent(session, data));

        return session;
    }

    public bool TryGetSession(long sessionId, out NetworkSession? session)
        => _sessions.TryGetValue(sessionId, out session);

    public IReadOnlyList<NetworkSession> GetSessions()
        => _sessions.Values.ToArray();

    public async Task<bool> SendAsync(long sessionId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetSession(sessionId, out var session) ||
            session is null ||
            session.Status != NetworkSessionStatusType.Connected ||
            !session.Connection.IsConnected)
        {
            return false;
        }

        await session.Connection.SendAsync(payload, cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<bool> CloseAsync(long sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetSession(sessionId, out var session) || session is null)
        {
            return false;
        }

        if (session.Status == NetworkSessionStatusType.Disconnected)
        {
            return false;
        }

        session.Status = NetworkSessionStatusType.Closing;

        try
        {
            await session.Connection.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to close network session {SessionId}", sessionId);

            throw;
        }

        return true;
    }
}
