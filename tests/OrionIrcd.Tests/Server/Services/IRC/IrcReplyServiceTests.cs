using System.Text;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Server.Services.Sessions;
using OrionIrcd.Tests.Support.Events;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.IRC;

public class IrcReplyServiceTests
{
    [Fact]
    public async Task SendLineAsync_WithConnectedSession_AppendsCrlfAndSendsUtf8()
    {
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = sessionManager.Register(connection);
        var service = new IrcReplyService(sessionManager);

        var result = await service.SendLineAsync(session, "PONG :abc", CancellationToken.None);

        Assert.True(result);
        Assert.Equal("PONG :abc\r\n", Encoding.UTF8.GetString(Assert.Single(connection.SentPayloads)));
    }

    [Fact]
    public async Task SendNumericAsync_WithNickname_UsesServerPrefix()
    {
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = sessionManager.Register(connection);
        var service = new IrcReplyService(sessionManager);

        await service.SendNumericAsync(
            session,
            "001",
            "squid",
            "Welcome to OrionIRCd squid",
            CancellationToken.None
        );

        Assert.Equal(
            ":orionircd 001 squid :Welcome to OrionIRCd squid\r\n",
            Encoding.UTF8.GetString(Assert.Single(connection.SentPayloads))
        );
    }
}
