using System.Globalization;
using System.Windows.Data;

namespace SmartKnowledgeHub.App.Converters;

public sealed class UtcToLocalConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime utc)
        {
            return string.Empty;
        }

        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        var format = parameter as string ?? "g";
        return local.ToString(format, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
