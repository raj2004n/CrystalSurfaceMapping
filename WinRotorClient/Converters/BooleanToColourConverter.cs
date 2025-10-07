using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WinRotor.Converters;

public class BooleanToColourConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Keys for dynamic resources
        const string connectedColorKey = "Secondary";
        const string disconnectedColorKey = "Error";

        // Check if the value is a boolean
        if (value is not bool isConnected) return Brushes.Gray;
        
        // Use a ternary operator to choose the correct resource key
        var resourceKey = isConnected ? connectedColorKey : disconnectedColorKey;
            
        // Try to find the resource and return it if found
        if (Application.Current is { } app && app.TryFindResource(resourceKey, out var resourceValue))
        {
            return resourceValue as IBrush;
        }

        // Fallback to a static color if the resource is not found
        return Brushes.DarkBlue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotImplementedException();
    
}