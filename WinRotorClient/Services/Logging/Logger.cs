using System;
using System.Collections.ObjectModel;
using WinRotor.ViewModels;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace WinRotor.Services.Logging;

public class Logger(MapViewModel mapViewModel) : ILogger
{
    private ObservableCollection<string> CommandLog { get; } = [];
    private ObservableCollection<string> DescriptionLog { get; } = [];

    public void LogCommand(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            mapViewModel.CmdLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        });
    
    }

    public void LogDescription(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            mapViewModel.DescLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        });
    }

}