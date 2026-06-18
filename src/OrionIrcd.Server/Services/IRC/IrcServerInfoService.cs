using OrionIrcd.Core.Data.Config;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC;

public sealed class IrcServerInfoService : IIrcServerInfoService
{
    private const string DefaultServerName = "irc.orionircd.net";

    private readonly OrionIrcdConfig _config;

    public IrcServerInfoService(OrionIrcdConfig config)
    {
        _config = config;
    }

    public string ServerName
        => string.IsNullOrWhiteSpace(_config.ServerName) ? DefaultServerName : _config.ServerName;
}
