using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Services.Sessions;
using OrionIrcd.Server.Types;
using OrionIrcd.Tests.Support.Events;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.Sessions;

public class SessionManagerServiceTests
{
    [Fact]
    public void NetworkSession_Constructor_InitializesConnectedState()
    {
        var connection = new TestNetworkConnection { SessionId = 42 };
        var connectedAt = new DateTimeOffset(2026, 6, 18, 10, 0, 0, TimeSpan.Zero);

        var session = new NetworkSession(
            connection.SessionId,
            connection,
            connection.RemoteEndPoint,
            connectedAt
        );

        Assert.Equal(42, session.SessionId);
        Assert.Same(connection, session.Connection);
        Assert.Equal(connection.RemoteEndPoint, session.RemoteEndPoint);
        Assert.Equal(connectedAt, session.ConnectedAtUtc);
        Assert.Equal(connectedAt, session.LastActivityAtUtc);
        Assert.Equal(0, session.BytesReceived);
        Assert.Equal(NetworkSessionStatusType.Connected, session.Status);
    }

    [Fact]
    public void NetworkSessionEvents_ConstructedWithSession_ExposeSessionAndPayload()
    {
        var connection = new TestNetworkConnection { SessionId = 42 };
        var session = new NetworkSession(
            connection.SessionId,
            connection,
            connection.RemoteEndPoint,
            DateTimeOffset.UnixEpoch
        );
        ReadOnlyMemory<byte> payload = new byte[] { 1, 2, 3 };

        var connected = new NetworkSessionConnectedEvent(session);
        var disconnected = new NetworkSessionDisconnectedEvent(session);
        var dataReceived = new NetworkSessionDataReceivedEvent(session, payload);

        Assert.Same(session, connected.Session);
        Assert.Same(session, disconnected.Session);
        Assert.Same(session, dataReceived.Session);
        Assert.Equal(new byte[] { 1, 2, 3 }, dataReceived.Data.ToArray());
    }

    [Fact]
    public async Task Register_NewConnection_AddsSessionAndPublishesConnectedEvent()
    {
        var eventBus = new RecordingEventBus();
        var service = new SessionManagerService(eventBus, TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };

        var session = service.Register(connection);
        var publishedEvent = await eventBus.WaitForEventAsync<NetworkSessionConnectedEvent>(
            TimeSpan.FromSeconds(5)
        );

        Assert.Same(session, publishedEvent.Session);
        Assert.True(service.TryGetSession(10, out var foundSession));
        Assert.Same(session, foundSession);
        Assert.Equal(NetworkSessionStatusType.Connected, session.Status);
    }

    [Fact]
    public async Task Unregister_ExistingConnection_RemovesSessionAndPublishesDisconnectedEvent()
    {
        var eventBus = new RecordingEventBus();
        var service = new SessionManagerService(eventBus, TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = service.Register(connection);

        var removedSession = service.Unregister(connection);
        var publishedEvent = await eventBus.WaitForEventAsync<NetworkSessionDisconnectedEvent>(
            TimeSpan.FromSeconds(5)
        );

        Assert.Same(session, removedSession);
        Assert.Same(session, publishedEvent.Session);
        Assert.Equal(NetworkSessionStatusType.Disconnected, session.Status);
        Assert.False(service.TryGetSession(10, out _));
    }

    [Fact]
    public async Task RecordActivity_ExistingConnection_UpdatesCountersAndPublishesDataEvent()
    {
        var eventBus = new RecordingEventBus();
        var service = new SessionManagerService(eventBus, TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = service.Register(connection);
        ReadOnlyMemory<byte> payload = new byte[] { 1, 2, 3, 4 };

        var updatedSession = service.RecordActivity(connection, payload);
        var publishedEvent = await eventBus.WaitForEventAsync<NetworkSessionDataReceivedEvent>(
            TimeSpan.FromSeconds(5)
        );

        Assert.Same(session, updatedSession);
        Assert.Equal(4, session.BytesReceived);
        Assert.True(session.LastActivityAtUtc >= session.ConnectedAtUtc);
        Assert.Same(session, publishedEvent.Session);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, publishedEvent.Data.ToArray());
    }

    [Fact]
    public void RecordActivity_UnknownConnection_RegistersSessionBeforeUpdatingActivity()
    {
        var service = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };

        var session = service.RecordActivity(connection, new byte[] { 1, 2, 3 });

        Assert.NotNull(session);
        Assert.True(service.TryGetSession(10, out var foundSession));
        Assert.Same(session, foundSession);
        Assert.Equal(3, session!.BytesReceived);
    }

    [Fact]
    public async Task SendAsync_ConnectedSession_SendsPayloadAndReturnsTrue()
    {
        var service = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        service.Register(connection);

        var result = await service.SendAsync(10, new byte[] { 1, 2, 3 }, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.Single(connection.SentPayloads));
    }

    [Fact]
    public async Task SendAsync_MissingSession_ReturnsFalse()
    {
        var service = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);

        var result = await service.SendAsync(10, new byte[] { 1, 2, 3 }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task CloseAsync_ConnectedSession_MarksClosingAndClosesConnection()
    {
        var service = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = service.Register(connection);

        var result = await service.CloseAsync(10, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(NetworkSessionStatusType.Closing, session.Status);
        Assert.False(connection.IsConnected);
        Assert.Equal(1, connection.CloseCallCount);
    }

    [Fact]
    public async Task StopAsync_RegisteredSessions_ClosesAndClearsSessions()
    {
        var service = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var first = new TestNetworkConnection { SessionId = 10 };
        var second = new TestNetworkConnection { SessionId = 11 };
        service.Register(first);
        service.Register(second);

        await service.StopAsync(CancellationToken.None);

        Assert.Empty(service.GetSessions());
        Assert.False(first.IsConnected);
        Assert.False(second.IsConnected);
    }
}
