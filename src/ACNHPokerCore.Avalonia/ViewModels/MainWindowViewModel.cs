using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreLib = ACNHPokerCore.Core;

namespace ACNHPokerCore.Avalonia.ViewModels;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Failed,
}

/// <summary>
/// Backs MainWindow. Ported behavior: Main.cs's StartConnectionButton_Click opened a raw
/// TCP Socket to port 6000 on a background Thread and polled BeginConnect/EndConnect with
/// a 3s timeout; this does the same thing over the wire (same port, same sys-botbase
/// handshake via ACNHPokerCore.Core.Utilities.CheckSysBotBase) but with async/await and a
/// CancellationToken instead of a raw Thread, since there's no WinForms Invoke() to fight
/// with here.
///
/// Deliberate deviation from the original: IP input is validated with
/// <see cref="IPAddress.TryParse"/> instead of the original's regex - simpler and it
/// also accepts IPv6, which the regex never could.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private Socket? _socket;

    [ObservableProperty]
    private string ipAddress = "192.168.1.";

    [ObservableProperty]
    private ConnectionState state = ConnectionState.Disconnected;

    [ObservableProperty]
    private string statusText = "Not connected";

    [ObservableProperty]
    private string logText = "Welcome to ACNHPokerCore for Linux (Avalonia port, in progress).\n" +
                              "Enter your Switch's IP address (Settings > Internet > Connection Status on the Switch) and press Connect.\n";

    public bool IsConnected => State == ConnectionState.Connected;

    public MainWindowViewModel()
    {
        CoreLib.MessageBox.ErrorReported += (text, caption) =>
            AppendLog(string.IsNullOrEmpty(caption) ? text : $"[{caption}] {text}");

        CoreLib.MyMessageBox.MessageRequested += (text, caption, _, _) =>
            AppendLog(string.IsNullOrEmpty(caption) ? text : $"[{caption}] {text}");
    }

    private void AppendLog(string line) => LogText += line + "\n";

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (State == ConnectionState.Connecting)
            return;

        if (!IPAddress.TryParse(IpAddress.Trim(), out IPAddress? address))
        {
            StatusText = "Invalid IP address";
            State = ConnectionState.Failed;
            return;
        }

        State = ConnectionState.Connecting;
        StatusText = $"Connecting to {address}:6000 ...";

        _socket?.Dispose();
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            var endpoint = new IPEndPoint(address, 6000);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _socket.ConnectAsync(endpoint, cts.Token);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            State = ConnectionState.Failed;
            StatusText = "Sys-botbase not responding.";
            AppendLog($"Connection failed: {ex.Message}");
            AppendLog("Checklist: Switch running Atmosphere/CFW, sys-botbase installed at " +
                      "atmosphere/contents/430000000000000B on the SD card, correct IP address, " +
                      "game booted to the title screen or later.");
            _socket.Dispose();
            _socket = null;
            return;
        }

        State = ConnectionState.Connected;
        StatusText = $"Connected to {address}";
        AppendLog($"Connection succeeded: {address}");

        // Same handshake as the original StartConnectionButton_Click: ask sys-botbase for
        // its version string over the socket we just opened.
        try
        {
            string version = await Task.Run(() => CoreLib.Utilities.CheckSysBotBase(_socket, usb: null));
            AppendLog($"sys-botbase version: {version}");
        }
        catch (Exception ex)
        {
            AppendLog($"Could not read sys-botbase version: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        _socket?.Dispose();
        _socket = null;
        State = ConnectionState.Disconnected;
        StatusText = "Not connected";
        AppendLog("Disconnected.");
    }

    /// <summary>Every feature button that isn't wired up yet routes here so the button grid
    /// can be laid out now and filled in screen-by-screen in later sessions.</summary>
    [RelayCommand]
    private void OpenStub(string? featureName)
    {
        AppendLog($"\"{featureName}\" is not ported yet - see README.md TODO list.");
    }
}
