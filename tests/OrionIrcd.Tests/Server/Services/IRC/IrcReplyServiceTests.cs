using System.Text;
using OrionIrcd.Core.Data.Config;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Server.Core.Services.Sessions;
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
        var service = new IrcReplyService(sessionManager, CreateConfig("orionircd"));

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
        var service = new IrcReplyService(sessionManager, CreateConfig("orionircd"));

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

    [Fact]
    public async Task SendNumericAsync_WithCustomConfig_UsesConfiguredServerName()
    {
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = sessionManager.Register(connection);
        var service = new IrcReplyService(sessionManager, CreateConfig("irc.example.net"));

        await service.SendNumericAsync(
            session,
            "001",
            "squid",
            "Welcome to OrionIRCd squid",
            CancellationToken.None
        );

        Assert.Equal(
            ":irc.example.net 001 squid :Welcome to OrionIRCd squid\r\n",
            Encoding.UTF8.GetString(Assert.Single(connection.SentPayloads))
        );
    }

    [Fact]
    public async Task SendReplyAsync_WithTypedReply_UsesConfiguredServerName()
    {
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = sessionManager.Register(connection);
        var service = new IrcReplyService(sessionManager, CreateConfig("irc.example.net"));

        var result = await service.SendReplyAsync(
            session,
            IrcReplies.NeedMoreParameters("USER"),
            CancellationToken.None
        );

        Assert.True(result);
        Assert.Equal(
            ":irc.example.net 461 * USER :Not enough parameters\r\n",
            Encoding.UTF8.GetString(Assert.Single(connection.SentPayloads))
        );
    }

    [Fact]
    public void ServerName_WithConfig_ReturnsConfiguredServerName()
    {
        var service = new IrcReplyService(
            new SessionManagerService(new RecordingEventBus(), TimeProvider.System),
            CreateConfig("irc.config.net")
        );

        Assert.Equal("irc.config.net", service.ServerName);
    }

    [Fact]
    public void ServerName_WithBlankConfig_ReturnsDefaultServerName()
    {
        var service = new IrcReplyService(
            new SessionManagerService(new RecordingEventBus(), TimeProvider.System),
            CreateConfig(string.Empty)
        );

        Assert.Equal("irc.orionircd.net", service.ServerName);
    }

    private static OrionIrcdConfig CreateConfig(string serverName)
        => new() { ServerName = serverName };
}
