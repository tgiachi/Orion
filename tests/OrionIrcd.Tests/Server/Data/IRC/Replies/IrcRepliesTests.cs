using OrionIrcd.Server.Data.IRC.Replies;

namespace OrionIrcd.Tests.Server.Data.IRC.Replies;

public class IrcRepliesTests
{
    [Fact]
    public void CapabilityList_WithServerName_FormatsCapLs()
    {
        var line = IrcReplies.CapabilityList().Format(CreateContext());

        Assert.Equal(":orionircd CAP * LS :", line);
    }

    [Fact]
    public void CapabilityNak_WithCapabilities_FormatsCapNak()
    {
        var line = IrcReplies.CapabilityNak(["multi-prefix", "sasl"]).Format(CreateContext());

        Assert.Equal(":orionircd CAP * NAK :multi-prefix sasl", line);
    }

    [Fact]
    public void ClosingLink_WithReason_FormatsError()
    {
        var line = IrcReplies.ClosingLink("Client Quit").Format(CreateContext());

        Assert.Equal("ERROR :Closing Link: Client Quit", line);
    }

    [Fact]
    public void NeedMoreParameters_WithCommand_FormatsNumeric461()
    {
        var line = IrcReplies.NeedMoreParameters("USER").Format(CreateContext());

        Assert.Equal(":orionircd 461 * USER :Not enough parameters", line);
    }

    [Fact]
    public void NicknameInUse_WithNickname_FormatsNumeric433()
    {
        var line = IrcReplies.NicknameInUse("squid").Format(CreateContext());

        Assert.Equal(":orionircd 433 * squid :Nickname is already in use", line);
    }

    [Fact]
    public void NoNicknameGiven_FormatsNumeric431()
    {
        var line = IrcReplies.NoNicknameGiven().Format(CreateContext());

        Assert.Equal(":orionircd 431 * :No nickname given", line);
    }

    [Fact]
    public void Pong_WithToken_FormatsServerPong()
    {
        var line = IrcReplies.Pong("abc123").Format(CreateContext());

        Assert.Equal(":orionircd PONG orionircd :abc123", line);
    }

    [Fact]
    public void PasswordMismatch_FormatsNumeric464()
    {
        var line = IrcReplies.PasswordMismatch().Format(CreateContext());

        Assert.Equal(":orionircd 464 * :Password incorrect", line);
    }

    [Fact]
    public void Welcome_WithNickname_FormatsNumeric001()
    {
        var line = IrcReplies.Welcome("squid").Format(CreateContext());

        Assert.Equal(":orionircd 001 squid :Welcome to OrionIRCd squid", line);
    }

    [Fact]
    public void YourHost_WithVersion_FormatsNumeric002()
    {
        var line = IrcReplies.YourHost("squid", "1.2.3").Format(CreateContext());

        Assert.Equal(":orionircd 002 squid :Your host is orionircd, running version 1.2.3", line);
    }

    [Fact]
    public void Created_WithMessage_FormatsNumeric003()
    {
        var line = IrcReplies.Created("squid", "This server was created for OrionIRCd").Format(CreateContext());

        Assert.Equal(":orionircd 003 squid :This server was created for OrionIRCd", line);
    }

    [Fact]
    public void MyInfo_WithVersion_FormatsNumeric004()
    {
        var line = IrcReplies.MyInfo("squid", "1.2.3").Format(CreateContext());

        Assert.Equal(":orionircd 004 squid orionircd OrionIRCd 1.2.3 o o", line);
    }

    [Fact]
    public void ISupport_WithDefaults_FormatsNumeric005()
    {
        var line = IrcReplies.ISupport("squid").Format(CreateContext());

        Assert.Equal(":orionircd 005 squid CHANTYPES=# NICKLEN=30 :are supported by this server", line);
    }

    [Fact]
    public void MotdStart_WithTarget_FormatsNumeric375()
    {
        var line = IrcReplies.MotdStart("squid").Format(CreateContext());

        Assert.Equal(":orionircd 375 squid :- orionircd Message of the day -", line);
    }

    [Fact]
    public void MotdLine_WithTargetAndLine_FormatsNumeric372()
    {
        var line = IrcReplies.MotdLine("squid", "Welcome").Format(CreateContext());

        Assert.Equal(":orionircd 372 squid :- Welcome", line);
    }

    [Fact]
    public void EndOfMotd_WithTarget_FormatsNumeric376()
    {
        var line = IrcReplies.EndOfMotd("squid").Format(CreateContext());

        Assert.Equal(":orionircd 376 squid :End of /MOTD command.", line);
    }

    [Fact]
    public void NoMotd_WithTarget_FormatsNumeric422()
    {
        var line = IrcReplies.NoMotd("squid").Format(CreateContext());

        Assert.Equal(":orionircd 422 squid :MOTD File is missing", line);
    }

    private static IrcReplyContext CreateContext()
        => new("orionircd");
}
