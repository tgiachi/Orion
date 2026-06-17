using OrionIrcd.Core.Utils;

namespace OrionIrcd.Core.Extensions.Strings;

/// <summary>
/// Provides extension methods for string operations, particularly for case conversions.
/// </summary>
public static class StringMethodExtension
{
    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A camelCase version of the input string.</returns>
    public static string ToCamelCase(this string text)
        => StringUtils.ToCamelCase(text);

    /// <summary>
    /// Converts a string to Dot Case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A Dot Case version of the input string.</returns>
    public static string ToDotCase(this string text)
        => StringUtils.ToDotCase(text);

    /// <summary>
    /// Converts a string to kebab-case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A kebab-case version of the input string.</returns>
    public static string ToKebabCase(this string text)
        => StringUtils.ToKebabCase(text);

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A PascalCase version of the input string.</returns>
    public static string ToPascalCase(this string text)
        => StringUtils.ToPascalCase(text);

    /// <summary>
    /// Converts a string to Path Case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A Path Case version of the input string.</returns>
    public static string ToPathCase(this string text)
        => StringUtils.ToPathCase(text);

    /// <summary>
    /// Converts a string to Sentence Case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A Sentence Case version of the input string.</returns>
    public static string ToSentenceCase(this string text)
        => StringUtils.ToSentenceCase(text);

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A snake_case version of the input string.</returns>
    public static string ToSnakeCase(this string text)
        => StringUtils.ToSnakeCase(text);

    /// <summary>
    /// Converts a string to UPPER_SNAKE_CASE.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>An UPPER_SNAKE_CASE version of the input string.</returns>
    public static string ToSnakeCaseUpper(this string text)
        => StringUtils.ToUpperSnakeCase(text);

    /// <summary>
    /// Converts a string to Title Case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A Title Case version of the input string.</returns>
    public static string ToTitleCase(this string text)
        => StringUtils.ToTitleCase(text);

    /// <summary>
    /// Converts a string to Train Case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>A Train Case version of the input string.</returns>
    public static string ToTrainCase(this string text)
        => StringUtils.ToTrainCase(text);
}
