using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SuperPOS.Client.Views.Ai;

/// <summary>Visible si el string no es nulo ni vacío (o no vacío luego de trim).</summary>
public sealed class IaStringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        if (s is not null) s = s.Trim();
        return !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
