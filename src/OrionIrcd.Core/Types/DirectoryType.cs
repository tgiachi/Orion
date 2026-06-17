namespace OrionIrcd.Core.Types;

/// <summary>
/// Defines the types of directories used by the game engine
/// Used for organizing and accessing different types of game content
/// </summary>
public enum DirectoryType
{
    /// <summary>Directory for storing log files</summary>
    Logs,

    /// <summary>Directory for storing game assets like textures, sounds, and fonts</summary>
    /// <summary>
    /// Directory for storing script files used by the game engine
    /// </summary>
    Scripts,

    /// <summary>
    /// Where certs lives
    /// </summary>

    Certs,

    /// <summary>
    /// Future Db
    /// </summary>
    Data,
}
