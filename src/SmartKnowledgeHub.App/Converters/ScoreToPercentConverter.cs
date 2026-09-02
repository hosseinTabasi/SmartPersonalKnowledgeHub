using System.Globalization;
using System.Windows.Data;

namespace SmartKnowledgeHub.App.Converters;

public sealed class ScoreToPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double score)
        {
            return string.Empty;
        }

        if (score <= 0)
        {
            return score.ToString("0.###", culture);
        }

        if (score <= 1.0001)
        {
            return (score * 100).ToString("0.0", culture) + "%";
        }

        return score.ToString("0.###", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
