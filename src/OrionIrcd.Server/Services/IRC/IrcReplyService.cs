using System.Text;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC;

public sealed class IrcReplyService : IIrcReplyService
{
    private const string Crlf = "\r\n";
    private const string DefaultServerName = "orionircd";

    private readonly ISessionManagerService _sessionManagerService;

    public IrcReplyService(ISessionManagerService sessionManagerService)
    {
        _sessionManagerService = sessionManagerService;
    }

    public string ServerName => DefaultServerName;

    public Task<bool> SendLineAsync(NetworkSession session, string line, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(line))
        {
            return Task.FromResult(false);
        }

        var payload = Encoding.UTF8.GetBytes(line + Crlf);

        return _sessionManagerService.SendAsync(session.SessionId, payload, cancellationToken);
    }

    public Task<bool> SendNumericAsync(
        NetworkSession session,
        string code,
        string target,
        string message,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeTarget = string.IsNullOrWhiteSpace(target) ? "*" : target;
        var line = $":{ServerName} {code} {safeTarget} :{message}";

        return SendLineAsync(session, line, cancellationToken);
    }
}
