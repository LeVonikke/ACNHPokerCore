using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ACNHPokerCore.Avalonia.ViewModels;

/// <summary>Maps <see cref="ConnectionState"/> to the same red/orange/green indicator
/// colors Main.cs used on IPAddressInputBackground.BackColor.</summary>
public sealed class ConnectionStateToColorConverter : IValueConverter
{
    public static readonly ConnectionStateToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ConnectionState.Connected => Colors.LimeGreen,
        ConnectionState.Connecting => Colors.Orange,
        ConnectionState.Failed => Colors.Red,
        _ => Colors.Gray,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
