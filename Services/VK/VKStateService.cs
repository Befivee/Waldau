using System.Collections.Concurrent;

namespace WaldauCastle.Services.VK;

public class VKStateService
{
    private readonly ConcurrentDictionary<long, VKUserSession> _sessions = new();

    public VKUserSession GetOrCreate(long userId) =>
        _sessions.GetOrAdd(userId, _ => new VKUserSession());
}
