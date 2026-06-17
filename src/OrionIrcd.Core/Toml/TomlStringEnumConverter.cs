using Tomlyn.Serialization;

namespace OrionIrcd.Core.Toml;

internal sealed class TomlStringEnumConverter<TEnum> : TomlConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(TomlReader reader)
    {
        if (reader.TokenType == TomlTokenType.String)
        {
            var value = reader.GetString();

            return Enum.TryParse(value, true, out TEnum parsedValue)
                       ? parsedValue
                       : throw reader.CreateException($"Invalid {typeof(TEnum).Name} value '{value}'.");
        }

        if (reader.TokenType == TomlTokenType.Integer)
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), reader.GetInt64());
        }

        throw reader.CreateException($"Expected string value for {typeof(TEnum).Name}.");
    }

    public override void Write(TomlWriter writer, TEnum value)
        => writer.WriteStringValue(value.ToString());
}
