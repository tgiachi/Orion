using Tomlyn;
using Tomlyn.Serialization;

namespace OrionIrcd.Core.Toml;

/// <summary>
/// Provides TOML serialization helpers.
/// </summary>
public static class TomlUtils
{
    private static readonly TomlSerializerOptions DefaultOptions = new()
    {
        Converters = [new TomlStringEnumConverterFactory()]
    };

    /// <summary>
    /// Deserializes TOML text using reflection-based metadata.
    /// </summary>
    /// <param name="toml">The TOML text to deserialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T Deserialize<T>(string toml, TomlSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toml);

        return TomlSerializer.Deserialize<T>(toml, options ?? DefaultOptions) ??
               throw new TomlException($"Deserialization returned null for type {typeof(T).Name}");
    }

    /// <summary>
    /// Deserializes TOML text using source-generated type metadata.
    /// </summary>
    /// <param name="toml">The TOML text to deserialize.</param>
    /// <param name="typeInfo">The source-generated TOML type information.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T Deserialize<T>(string toml, TomlTypeInfo<T> typeInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toml);
        ArgumentNullException.ThrowIfNull(typeInfo);

        return TomlSerializer.Deserialize(toml, typeInfo) ??
               throw new TomlException($"Deserialization returned null for type {typeof(T).Name}");
    }

    /// <summary>
    /// Deserializes TOML from a file using reflection-based metadata.
    /// </summary>
    /// <param name="filePath">The TOML file path.</param>
    /// <param name="options">The serializer options.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T DeserializeFromFile<T>(string filePath, TomlSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var toml = File.ReadAllText(GetExistingFilePath(filePath));

        return Deserialize<T>(toml, options);
    }

    /// <summary>
    /// Deserializes TOML from a file using source-generated type metadata.
    /// </summary>
    /// <param name="filePath">The TOML file path.</param>
    /// <param name="typeInfo">The source-generated TOML type information.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T DeserializeFromFile<T>(string filePath, TomlTypeInfo<T> typeInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(typeInfo);

        var toml = File.ReadAllText(GetExistingFilePath(filePath));

        return Deserialize(toml, typeInfo);
    }

    /// <summary>
    /// Serializes an object to TOML using reflection-based metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <typeparam name="T">The source type.</typeparam>
    /// <returns>The serialized TOML text.</returns>
    public static string Serialize<T>(T obj, TomlSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return TomlSerializer.Serialize(obj, options ?? DefaultOptions);
    }

    /// <summary>
    /// Serializes an object to TOML using source-generated type metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="typeInfo">The source-generated TOML type information.</param>
    /// <typeparam name="T">The source type.</typeparam>
    /// <returns>The serialized TOML text.</returns>
    public static string Serialize<T>(T obj, TomlTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(typeInfo);

        return TomlSerializer.Serialize(obj, typeInfo);
    }

    /// <summary>
    /// Serializes an object to a TOML file using reflection-based metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="filePath">The output TOML file path.</param>
    /// <param name="options">The serializer options.</param>
    /// <typeparam name="T">The source type.</typeparam>
    public static void SerializeToFile<T>(T obj, string filePath, TomlSerializerOptions? options = null)
    {
        var toml = Serialize(obj, options);

        File.WriteAllText(GetWritableFilePath(filePath), toml);
    }

    /// <summary>
    /// Serializes an object to a TOML file using source-generated type metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="filePath">The output TOML file path.</param>
    /// <param name="typeInfo">The source-generated TOML type information.</param>
    /// <typeparam name="T">The source type.</typeparam>
    public static void SerializeToFile<T>(T obj, string filePath, TomlTypeInfo<T> typeInfo)
    {
        var toml = Serialize(obj, typeInfo);

        File.WriteAllText(GetWritableFilePath(filePath), toml);
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
