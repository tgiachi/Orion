using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace OrionIrcd.Network.Encryption;

/// <summary>
///     Represents login encryption keys derived from the client version.
/// </summary>
public readonly struct LoginKeys
{
    private static readonly ConcurrentDictionary<(int Major, int Minor, int Revision), LoginKeys> _cache = new();

    private static readonly Lazy<LoginKeys[]> _legacyKeys =
        new(BuildLegacyKeys, LazyThreadSafetyMode.ExecutionAndPublication);

    private LoginKeys(uint key1, uint key2)
    {
        Key1 = key1;
        Key2 = key2;
    }

    /// <summary>
    ///     Gets the first login key.
    /// </summary>
    public uint Key1 { get; }

    /// <summary>
    ///     Gets the second login key.
    /// </summary>
    public uint Key2 { get; }

    /// <summary>
    ///     Gets the precomputed legacy key list for pre-6.0.5 clients.
    /// </summary>
    public static ReadOnlySpan<LoginKeys> LegacyKeys => _legacyKeys.Value;

    /// <summary>
    ///     Computes or retrieves login keys for a client version.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LoginKeys GetKeys(int major, int minor, int revision)
    {
        return _cache.GetOrAdd(
            (major, minor, revision),
            static key => ComputeKeys((uint)key.Major, (uint)key.Minor, (uint)key.Revision)
        );
    }

    private static LoginKeys[] BuildLegacyKeys()
    {
        ReadOnlySpan<(uint Major, uint Minor, uint Revision)> versions =
        [
            (4, 0, 11),
            (5, 0, 0), (5, 0, 1), (5, 0, 2), (5, 0, 3), (5, 0, 4),
            (5, 0, 5), (5, 0, 6), (5, 0, 7), (5, 0, 8), (5, 0, 9),
            (6, 0, 0), (6, 0, 1), (6, 0, 2), (6, 0, 3), (6, 0, 4)
        ];

        var keys = new LoginKeys[versions.Length];

        for (var i = 0; i < versions.Length; i++)
        {
            keys[i] = ComputeKeys(versions[i].Major, versions[i].Minor, versions[i].Revision);
        }

        return keys;
    }

    private static LoginKeys ComputeKeys(uint major, uint minor, uint revision)
    {
        var key1 = (major << 23) | (minor << 14) | (revision << 4);
        key1 ^= (revision * revision) << 9;
        key1 ^= minor * minor;
        key1 ^= (minor * 11) << 24;
        key1 ^= (revision * 7) << 19;
        key1 ^= 0x2C13A5FD;

        var key2 = (major << 22) | (revision << 13) | (minor << 3);
        key2 ^= (revision * revision * 3) << 10;
        key2 ^= minor * minor;
        key2 ^= (minor * 13) << 23;
        key2 ^= (revision * 7) << 18;
        key2 ^= 0xA31D527F;

        return new LoginKeys(key1, key2);
    }
}
