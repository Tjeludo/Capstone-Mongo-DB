// In Converters/BooleanToVisibilityConverter.cs

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace App_v1.Converters; // This namespace must match

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return (value is Visibility v && v == Visibility.Visible);
    }
}