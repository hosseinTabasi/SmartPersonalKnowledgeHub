using System.Globalization;
using System.Windows.Data;
using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.App.Converters;

public sealed class PriorityToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var n = value is int i ? i : 1;
        return TaskPriorities.ToLabel(n);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
