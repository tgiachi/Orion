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

    private static IrcReplyContext CreateContext()
        => new("orionircd");
}
