using System;

using Microsoft.UI.Xaml.Data;

namespace BAAZ.CMMS.App.Converters;

/// <summary>true → 1.0, false → 0.45 (для отображения неактивных строк приглушённым цветом).</summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? 1.0 : 0.45;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
