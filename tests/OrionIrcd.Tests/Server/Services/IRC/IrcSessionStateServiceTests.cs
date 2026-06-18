using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.IRC;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.IRC;

public class IrcSessionStateServiceTests
{
    [Fact]
    public void IrcSessionRegisteredEvent_ConstructedWithSessionAndState_ExposesValues()
    {
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = new NetworkSession(
            connection.SessionId,
            connection,
            connection.RemoteEndPoint,
            DateTimeOffset.UnixEpoch
        );
        var state = new IrcSessionStateSnapshot
        {
            SessionId = 10,
            Nickname = "squid",
            Username = "squiduser",
            RealName = "Squid User",
            IsRegistered = true
        };

        var eventData = new IrcSessionRegisteredEvent(session, state);

        Assert.Same(session, eventData.Session);
        Assert.Same(state, eventData.State);
    }

    [Fact]
    public void Handle_WithDisconnectedSession_RemovesState()
    {
        var service = new IrcSessionStateService();
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = new NetworkSession(
            connection.SessionId,
            connection,
            connection.RemoteEndPoint,
            DateTimeOffset.UnixEpoch
        );
        service.TrySetNickname(10, "squid");

        service.Handle(new NetworkSessionDisconnectedEvent(session));

        Assert.False(service.TryGetSnapshot(10, out _));
    }

    [Fact]
    public void SetNickname_WhenNicknameIsUnused_ReturnsTrue()
    {
        var service = new IrcSessionStateService();

        var result = service.TrySetNickname(10, "squid");
        var snapshot = service.GetSnapshot(10);

        Assert.True(result);
        Assert.Equal("squid", snapshot.Nickname);
        Assert.False(snapshot.IsRegistered);
    }

    [Fact]
    public void TryMarkRegistered_WhenNicknameAndUserExist_ReturnsTrueOnce()
    {
        var service = new IrcSessionStateService();
        service.TrySetNickname(10, "squid");
        service.SetUser(10, "squiduser", "Squid User");

        var first = service.TryMarkRegistered(10, out var registered);
        var second = service.TryMarkRegistered(10, out _);

        Assert.True(first);
        Assert.NotNull(registered);
        Assert.Equal("squid", registered.Nickname);
        Assert.Equal("squiduser", registered.Username);
        Assert.True(registered.IsRegistered);
        Assert.False(second);
    }

    [Fact]
    public void TryMarkRegistered_WithRequiredPass_ReturnsFalseUntilPassAccepted()
    {
        var service = new IrcSessionStateService();
        service.TrySetNickname(10, "squid");
        service.SetUser(10, "squiduser", "Squid User");

        var beforePass = service.TryMarkRegistered(10, true, out _);
        service.SetPassAccepted(10);
        var afterPass = service.TryMarkRegistered(10, true, out var registered);

        Assert.False(beforePass);
        Assert.True(afterPass);
        Assert.NotNull(registered);
        Assert.True(registered.IsPassAccepted);
    }

    [Fact]
    public void TrySetNickname_WhenNicknameIsUsedByAnotherSession_ReturnsFalse()
    {
        var service = new IrcSessionStateService();
        service.TrySetNickname(10, "squid");

        var result = service.TrySetNickname(11, "squid");

        Assert.False(result);
    }
}
