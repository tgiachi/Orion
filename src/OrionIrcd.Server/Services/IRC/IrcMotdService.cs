using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Types;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC;

public sealed class IrcMotdService : IIrcMotdService
{
    private const string FilePrefix = "file://";

    private readonly OrionIrcdConfig _config;
    private readonly DirectoriesConfig _directoriesConfig;
    private readonly IIrcReplyService _replyService;

    public IrcMotdService(
        IIrcReplyService replyService,
        OrionIrcdConfig config,
        DirectoriesConfig directoriesConfig
    )
    {
        _replyService = replyService;
        _config = config;
        _directoriesConfig = directoriesConfig;
    }

    public IReadOnlyList<string> GetMotdLines()
    {
        if (string.IsNullOrWhiteSpace(_config.MOTD))
        {
            return [];
        }

        var motdText = _config.MOTD.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase)
                           ? ReadMotdFile(_config.MOTD[FilePrefix.Length..])
                           : _config.MOTD;

        return SplitLines(motdText);
    }

    public async Task SendMotdAsync(NetworkSession session, string target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeTarget = string.IsNullOrWhiteSpace(target) ? "*" : target;
        var lines = GetMotdLines();

        if (lines.Count == 0)
        {
            await _replyService.SendReplyAsync(session, IrcReplies.NoMotd(safeTarget), cancellationToken);

            return;
        }

        await _replyService.SendReplyAsync(session, IrcReplies.MotdStart(safeTarget), cancellationToken);

        foreach (var line in lines)
        {
            await _replyService.SendReplyAsync(session, IrcReplies.MotdLine(safeTarget, line), cancellationToken);
        }

        await _replyService.SendReplyAsync(session, IrcReplies.EndOfMotd(safeTarget), cancellationToken);
    }

    private string ReadMotdFile(string motdFile)
    {
        if (string.IsNullOrWhiteSpace(motdFile))
        {
            return string.Empty;
        }

        var path = Path.Combine(_directoriesConfig[DirectoryType.Data], motdFile.Trim());

        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static IReadOnlyList<string> SplitLines(string motdText)
        => motdText.Split(
                    ["\r\n", "\n"],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
}
