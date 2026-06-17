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
    Assets,

    /// <summary>Directory for storing game data and configuration files</summary>
    Data,

    /// <summary>
    /// Directory for storing script files used by the game engine
    /// </summary>
    Scripts,

    /// <summary>
    /// Directory for storing scene files that define the layout and content of game levels or environments
    /// </summary>
    Scenes,

    /// <summary>
    /// Directory for installed plugin folders (one subfolder per plugin Id).
    /// </summary>
    Plugins
}
