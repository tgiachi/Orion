using YamlDotNet.Serialization;

namespace OrionIrcd.Core.Yaml;

/// <summary>
/// Provides YAML serialization helpers.
/// </summary>
public static class YamlUtils
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .DisableAliases()
        .WithIndentedSequences()
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Deserializes YAML text using reflection-based metadata.
    /// </summary>
    /// <param name="yaml">The YAML text to deserialize.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T Deserialize<T>(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        return Deserializer.Deserialize<T>(yaml) ??
               throw new InvalidDataException($"Deserialization returned null for type {typeof(T).Name}");
    }

    /// <summary>
    /// Deserializes YAML from a file using reflection-based metadata.
    /// </summary>
    /// <param name="filePath">The YAML file path.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T DeserializeFromFile<T>(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var yaml = File.ReadAllText(GetExistingFilePath(filePath));

        return Deserialize<T>(yaml);
    }

    /// <summary>
    /// Serializes an object to YAML using reflection-based metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <typeparam name="T">The source type.</typeparam>
    /// <returns>The serialized YAML text.</returns>
    public static string Serialize<T>(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return Serializer.Serialize(obj);
    }

    /// <summary>
    /// Serializes an object to a YAML file using reflection-based metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="filePath">The output YAML file path.</param>
    /// <typeparam name="T">The source type.</typeparam>
    public static void SerializeToFile<T>(T obj, string filePath)
    {
        var yaml = Serialize(obj);

        File.WriteAllText(GetWritableFilePath(filePath), yaml);
    }

    private static string GetExistingFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var normalizedPath = Path.GetFullPath(filePath);

        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"The file '{normalizedPath}' does not exist.", normalizedPath);
        }

        return normalizedPath;
    }

    private static string GetWritableFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var normalizedPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(normalizedPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return normalizedPath;
    }
}
