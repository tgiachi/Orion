using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using OrionIrcd.Network.Interfaces.Encryption;

namespace OrionIrcd.Network.Encryption;

/// <summary>
///     Implements game transport encryption using Twofish and MD5-derived XOR state.
/// </summary>
public sealed class GameEncryption : IClientEncryption
{
    private const int CipherTableSize = 256;
    private const int BlockSize = 16;
    private const int GameLoginPacketSize = 65;

    private static readonly byte[] _identityTable = CreateIdentityTable();
    private readonly byte[] _cipherTable;

    private readonly TwofishEngine _twofish;
    private readonly byte[] _xorKey;
    private ushort _recvPos;
    private byte _sendPos;

    public GameEncryption(uint seed)
    {
        Span<byte> key = stackalloc byte[16];
        key[0] = key[4] = key[8] = key[12] = (byte)((seed >> 24) & 0xFF);
        key[1] = key[5] = key[9] = key[13] = (byte)((seed >> 16) & 0xFF);
        key[2] = key[6] = key[10] = key[14] = (byte)((seed >> 8) & 0xFF);
        key[3] = key[7] = key[11] = key[15] = (byte)(seed & 0xFF);

        _twofish = new TwofishEngine(key);
        _cipherTable = GC.AllocateUninitializedArray<byte>(CipherTableSize);
        _identityTable.CopyTo(_cipherTable, 0);
        RefreshCipherTable();
        _xorKey = CreateXorKey(_cipherTable);
    }

    public void ClientDecrypt(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            if (_recvPos >= CipherTableSize)
            {
                RefreshCipherTable();
            }

            buffer[i] ^= _cipherTable[_recvPos++];
        }
    }

    public void ServerEncrypt(Span<byte> buffer)
    {
        var i = 0;

        if (_sendPos == 0 && buffer.Length >= 16 && Vector128.IsHardwareAccelerated)
        {
            var keyVec = Vector128.Create(_xorKey);

            for (; i + 16 <= buffer.Length; i += 16)
            {
                var chunk = Vector128.LoadUnsafe(ref buffer[i]);
                var result = Vector128.Xor(chunk, keyVec);
                result.StoreUnsafe(ref buffer[i]);
            }
        }

        for (; i < buffer.Length; i++)
        {
            buffer[i] ^= _xorKey[_sendPos++];
            _sendPos &= 0x0F;
        }
    }

    public static bool TryDecrypt(uint seed, ReadOnlySpan<byte> encryptedPacket, out GameEncryption? encryption)
    {
        encryption = null;

        if (encryptedPacket.Length < GameLoginPacketSize)
        {
            return false;
        }

        Span<byte> decrypted = stackalloc byte[GameLoginPacketSize];
        encryptedPacket[..GameLoginPacketSize].CopyTo(decrypted);

        var candidate = new GameEncryption(seed);
        candidate.ClientDecrypt(decrypted);

        if (decrypted[0] != 0x91)
        {
            return false;
        }

        encryption = new GameEncryption(seed);

        return true;
    }

    private static byte[] CreateIdentityTable()
    {
        var table = new byte[CipherTableSize];

        for (var i = 0; i < CipherTableSize; i++)
        {
            table[i] = (byte)i;
        }

        return table;
    }

    [SuppressMessage(
        "Security",
        "CA5351:Do Not Use Broken Cryptographic Algorithms",
        Justification = "Ultima Online game encryption uses a legacy MD5-derived XOR stream for protocol compatibility."
    )]
    private static byte[] CreateXorKey(byte[] cipherTable)
    {
        return MD5.HashData(cipherTable);
    }

    private void RefreshCipherTable()
    {
        for (var i = 0; i < CipherTableSize; i += BlockSize)
        {
            _twofish.EncryptBlock(_cipherTable.AsSpan(i, BlockSize));
        }

        _recvPos = 0;
    }
}
