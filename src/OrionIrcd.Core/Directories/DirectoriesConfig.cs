using OrionIrcd.Core.Extensions.Strings;

namespace OrionIrcd.Core.Directories;

/// <summary>
/// Configuration for managing directory structures with automatic creation and path resolution
/// </summary>
public class DirectoriesConfig
{
    private readonly string[] _directories;

    /// <summary>
    /// Initializes a new instance of the DirectoriesConfig class.
    /// </summary>
    /// <param name="rootDirectory">The root directory path.</param>
    /// <param name="directories">The array of directory types.</param>
    public DirectoriesConfig(string rootDirectory, string[] directories)
    {
        _directories = directories;
        Root = rootDirectory;

        Init();
    }

    /// <summary>
    /// Gets the root directory path.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Gets the path for the specified directory type.
    /// </summary>
    /// <param name="directoryType">The directory type as string.</param>
    /// <returns>The path for the directory type.</returns>
    public string this[string directoryType] => GetPath(directoryType);

    /// <summary>
    /// Gets the path for the specified directory type enum.
    /// </summary>
    /// <param name="directoryType">The directory type enum.</param>
    /// <returns>The path for the directory type.</returns>
    public string this[Enum directoryType] => GetPath(directoryType.ToString());

    /// <summary>
    /// Gets the path for the specified directory type enum.
    /// </summary>
    /// <param name="value">The directory type enum value.</param>
    /// <returns>The path for the directory type.</returns>
    public string GetPath<TEnum>(TEnum value) where TEnum : struct, Enum
        => GetPath(Enum.GetName(value));

    /// <summary>
    /// Gets the path for the specified directory type string.
    /// </summary>
    /// <param name="directoryType">The directory type as string.</param>
    /// <returns>The path for the directory type.</returns>
    public string GetPath(string directoryType)
    {
        var path = Path.Combine(Root, directoryType.ToSnakeCase());

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }

    /// <summary>
    /// Returns a string representation of the root directory.
    /// </summary>
    /// <returns>The root directory path.</returns>
    public override string ToString()
        => Root;

    /// <summary>
    /// Initializes the directories configuration.
    /// </summary>
    private void Init()
    {
        if (!Directory.Exists(Root))
        {
            Directory.CreateDirectory(Root);
        }

        var directoryTypes = _directories.ToList();

        foreach (var path in directoryTypes.Select(GetPath)
                                           .Where(path => !Directory.Exists(path)))
        {
            Directory.CreateDirectory(path);
        }
    }
}
