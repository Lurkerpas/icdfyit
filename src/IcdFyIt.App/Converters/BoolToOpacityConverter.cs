using System.Globalization;
using Avalonia.Data.Converters;

namespace IcdFyIt.App.Converters;

/// <summary>Converts a bool to 1.0 (true/applicable) or 0.4 (false/not-applicable) opacity.</summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.4;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
