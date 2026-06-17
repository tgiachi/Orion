namespace OrionIrcd.Network.Types.Encryption;

/// <summary>
///     Specifies which transport encryption modes the server accepts.
/// </summary>
[Flags]
public enum EncryptionMode
{
    /// <summary>
    ///     Disables encryption support.
    /// </summary>
    None = 0x0,

    /// <summary>
    ///     Accepts unencrypted clients.
    /// </summary>
    Unencrypted = 0x1,

    /// <summary>
    ///     Accepts encrypted clients.
    /// </summary>
    Encrypted = 0x2,

    /// <summary>
    ///     Accepts both encrypted and unencrypted clients.
    /// </summary>
    Both = Unencrypted | Encrypted
}
