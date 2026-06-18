using OrionIrcd.Network.Interfaces.Encryption;

namespace OrionIrcd.Network.Encryption;

/// <summary>
/// Implements login packet encryption for account login traffic.
/// </summary>
public sealed class LoginEncryption : IClientEncryption
{
    private const int LoginPacketSize = 62;

    private readonly uint _key1;
    private readonly uint _key2;
    private uint _table1;
    private uint _table2;

    public LoginEncryption(uint seed, LoginKeys keys)
    {
        _key1 = keys.Key1;
        _key2 = keys.Key2;
        _table1 = ((~seed ^ 0x00001357) << 16) | ((seed ^ 0xFFFFAAAA) & 0x0000FFFF);
        _table2 = ((seed ^ 0x43210000) >> 16) | ((~seed ^ 0xABCDFFFF) & 0xFFFF0000);
    }

    public void ClientDecrypt(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] ^= (byte)(_table1 & 0xFF);

            var edx = _table2;
            var esi = _table1 << 31;
            var eax = _table2 >> 1;

            eax |= esi;
            eax ^= _key1 - 1;
            edx <<= 31;
            eax >>= 1;

            var ecx = _table1 >> 1;

            eax |= esi;
            ecx |= edx;
            eax ^= _key1;
            ecx ^= _key2;

            _table1 = ecx;
            _table2 = eax;
        }
    }

    public void ServerEncrypt(Span<byte> buffer)
        => _ = buffer;

    public static bool TryDecrypt(
        int? major,
        int? minor,
        int? revision,
        uint seed,
        ReadOnlySpan<byte> encryptedPacket,
        out LoginEncryption? encryption
    )
    {
        encryption = null;

        if (encryptedPacket.Length < LoginPacketSize)
        {
            return false;
        }

        if (major.HasValue && minor.HasValue && revision.HasValue)
        {
            var keys = LoginKeys.GetKeys(major.Value, minor.Value, revision.Value);

            return keys is not { Key1: 0, Key2: 0 } &&
                   TryDecryptWithKeys(keys, seed, encryptedPacket, out encryption);
        }

        foreach (var legacyKeys in LoginKeys.LegacyKeys)
        {
            if (TryDecryptWithKeys(legacyKeys, seed, encryptedPacket, out encryption))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryDecryptWithKeys(
        LoginKeys keys,
        uint seed,
        ReadOnlySpan<byte> encryptedPacket,
        out LoginEncryption? encryption
    )
    {
        Span<byte> decrypted = stackalloc byte[LoginPacketSize];
        encryptedPacket[..LoginPacketSize].CopyTo(decrypted);

        var candidate = new LoginEncryption(seed, keys);
        candidate.ClientDecrypt(decrypted);

        if (decrypted[0] != 0x80 || decrypted[30] != 0x00 || decrypted[60] != 0x00)
        {
            encryption = null;

            return false;
        }

        encryption = new(seed, keys);

        return true;
    }
}
