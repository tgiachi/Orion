using Tomlyn;
using Tomlyn.Serialization;

namespace OrionIrcd.Core.Toml;

internal sealed class TomlStringEnumConverterFactory : TomlConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

        return enumType.IsEnum;
    }

    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options)
    {
        var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        var converterType = typeof(TomlStringEnumConverter<>).MakeGenericType(enumType);

        return (TomlConverter)Activator.CreateInstance(converterType)!;
    }
}
