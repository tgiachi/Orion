using System.Reflection;

namespace OrionIrcd.Core.Utils;

/// <summary>
/// Provides utility methods for reading assembly version metadata.
/// </summary>
public static class VersionUtils
{
    /// <summary>
    /// Gets the informational version for the LyLy.Core assembly.
    /// </summary>
    /// <returns>The package version declared for LyLy.Core.</returns>
    public static string GetVersion()
        => GetVersion(typeof(VersionUtils).Assembly);

    /// <summary>
    /// Gets the informational version for the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to read version metadata from.</param>
    /// <returns>The assembly informational version, or the assembly version when informational metadata is unavailable.</returns>
    public static string GetVersion(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                           ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var metadataIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);

            return metadataIndex == -1 ? informationalVersion : informationalVersion[..metadataIndex];
        }

        return assembly.GetName().Version?.ToString() ?? string.Empty;
    }
}
