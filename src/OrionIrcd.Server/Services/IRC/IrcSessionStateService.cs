using System.Collections.Concurrent;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Server.Core.Data.Events;
using OrionIrcd.Server.Data.IRC;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC;

public sealed class IrcSessionStateService :
    IIrcSessionStateService,
    ISyncEventListener<NetworkSessionDisconnectedEvent>
{
    private readonly ConcurrentDictionary<long, IrcSessionState> _states = new();
    private readonly Lock _sync = new();

    public IrcSessionStateSnapshot GetSnapshot(long sessionId)
    {
        lock (_sync)
        {
            return CreateSnapshot(GetOrCreate(sessionId));
        }
    }

    public void Handle(NetworkSessionDisconnectedEvent eventData)
    {
        Remove(eventData.Session.SessionId);
    }

    public void Remove(long sessionId)
    {
        lock (_sync)
        {
            _states.TryRemove(sessionId, out _);
        }
    }

    public void SetUser(long sessionId, string username, string realName)
    {
        lock (_sync)
        {
            var state = GetOrCreate(sessionId);
            state.Username = username;
            state.RealName = realName;
        }
    }

    public void SetPassAccepted(long sessionId)
    {
        lock (_sync)
        {
            GetOrCreate(sessionId).IsPassAccepted = true;
        }
    }

    public bool TryGetSnapshot(long sessionId, out IrcSessionStateSnapshot? snapshot)
    {
        lock (_sync)
        {
            if (!_states.TryGetValue(sessionId, out var state))
            {
                snapshot = null;

                return false;
            }

            snapshot = CreateSnapshot(state);

            return true;
        }
    }

    public bool TryMarkRegistered(long sessionId, out IrcSessionStateSnapshot? snapshot)
        => TryMarkRegistered(sessionId, false, out snapshot);

    public bool TryMarkRegistered(long sessionId, bool isPassRequired, out IrcSessionStateSnapshot? snapshot)
    {
        lock (_sync)
        {
            var state = GetOrCreate(sessionId);

            if (state.IsRegistered ||
                string.IsNullOrWhiteSpace(state.Nickname) ||
                string.IsNullOrWhiteSpace(state.Username) ||
                (isPassRequired && !state.IsPassAccepted))
            {
                snapshot = null;

                return false;
            }

            state.IsRegistered = true;
            snapshot = CreateSnapshot(state);

            return true;
        }
    }

    public bool TrySetNickname(long sessionId, string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return false;
        }

        lock (_sync)
        {
            var normalized = nickname.Trim();

            foreach (var state in _states.Values)
            {
                if (state.SessionId != sessionId &&
                    string.Equals(state.Nickname, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            GetOrCreate(sessionId).Nickname = normalized;

            return true;
        }
    }

    private static IrcSessionStateSnapshot CreateSnapshot(IrcSessionState state)
        => new()
        {
            SessionId = state.SessionId,
            Nickname = state.Nickname,
            Username = state.Username,
            RealName = state.RealName,
            IsPassAccepted = state.IsPassAccepted,
            IsRegistered = state.IsRegistered
        };

    private IrcSessionState GetOrCreate(long sessionId)
        => _states.GetOrAdd(sessionId, static id => new() { SessionId = id });
}
