using System.Collections.Concurrent;
using Tomlyn;
using Tomlyn.Serialization;

namespace OrionIrcd.Core.Toml;

/// <summary>
/// Provides NativeAOT-friendly TOML serialization helpers.
/// </summary>
public static class TomlUtils
{
    private static readonly ConcurrentBag<TomlSerializerContext> TomlSerializerContexts = new();

    /// <summary>
    /// Deserializes TOML text using a source-generated serializer context.
    /// </summary>
    /// <param name="toml">The TOML text to deserialize.</param>
    /// <param name="context">The source-generated TOML serializer context.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T Deserialize<T>(string toml, TomlSerializerContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toml);
        ArgumentNullException.ThrowIfNull(context);

        return TomlSerializer.Deserialize<T>(toml, context) ??
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

        var normalizedPath = Path.GetFullPath(filePath);

        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"The file '{normalizedPath}' does not exist.", normalizedPath);
        }

        var toml = File.ReadAllText(normalizedPath);

        return Deserialize(toml, typeInfo);
    }

    /// <summary>
    /// Gets a read-only view of registered TOML serializer contexts.
    /// </summary>
    /// <returns>The registered TOML serializer contexts.</returns>
    public static IReadOnlyList<TomlSerializerContext> GetTomlContexts()
    {
        var contexts = new TomlSerializerContext[TomlSerializerContexts.Count];
        TomlSerializerContexts.CopyTo(contexts, 0);

        return Array.AsReadOnly(contexts);
    }

    /// <summary>
    /// Registers a TOML serializer context for source-generated metadata.
    /// </summary>
    /// <param name="context">The context to register.</param>
    public static void RegisterTomlContext(TomlSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        TomlSerializerContexts.Add(context);
    }

    /// <summary>
    /// Serializes an object to TOML using a source-generated serializer context.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="context">The source-generated TOML serializer context.</param>
    /// <typeparam name="T">The source type.</typeparam>
    /// <returns>The serialized TOML text.</returns>
    public static string Serialize<T>(T obj, TomlSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(context);

        return TomlSerializer.Serialize(obj, context);
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
    /// Serializes an object to a TOML file using source-generated type metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="filePath">The output TOML file path.</param>
    /// <param name="typeInfo">The source-generated TOML type information.</param>
    /// <typeparam name="T">The source type.</typeparam>
    public static void SerializeToFile<T>(T obj, string filePath, TomlTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(typeInfo);

        var normalizedPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(normalizedPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var toml = Serialize(obj, typeInfo);
        File.WriteAllText(normalizedPath, toml);
    }
}
