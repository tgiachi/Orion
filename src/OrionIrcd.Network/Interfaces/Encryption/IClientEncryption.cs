namespace OrionIrcd.Network.Interfaces.Encryption;

/// <summary>
/// Defines the contract for client transport encryption algorithms.
/// </summary>
public interface IClientEncryption
{
    /// <summary>
    /// Decrypts inbound client bytes in place.
    /// </summary>
    /// <param name="buffer">Encrypted bytes to decrypt.</param>
    void ClientDecrypt(Span<byte> buffer);

    /// <summary>
    /// Encrypts outbound server bytes in place.
    /// </summary>
    /// <param name="buffer">Plain bytes to encrypt.</param>
    void ServerEncrypt(Span<byte> buffer);
}
