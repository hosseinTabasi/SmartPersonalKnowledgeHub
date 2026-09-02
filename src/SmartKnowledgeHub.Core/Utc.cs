using System.Globalization;

namespace SmartKnowledgeHub.Core.Data;

internal static class Utc
{
    public static string Format(DateTime value) =>
        value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    public static string Format(DateTime? value) =>
        value is null ? string.Empty : Format(value.Value);

    public static DateTime Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DateTime.MinValue;
        }

        return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
    }

    public static DateTime? ParseOptional(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return Parse(text);
    }
}
