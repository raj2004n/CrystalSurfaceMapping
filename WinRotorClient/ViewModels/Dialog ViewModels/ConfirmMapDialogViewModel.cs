using System;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinRotor.ViewModels;

public partial class ConfirmMapDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = "Confirm";
    [ObservableProperty] private string _message = "Confirm message string.";
    [ObservableProperty] private string _confirmText = "OK";
    [ObservableProperty] private string _cancelText = "Cancel";
    [ObservableProperty] private bool _confirmed;
    [ObservableProperty] private string _currentTime = "00:00:00";

    private readonly DispatcherTimer _timer;
    
    // Direct reference to the window for immediate closure
    public Window? DialogWindow { get; set; }

    public ConfirmMapDialogViewModel(string title, string message, TimeSpan estimatedTime)
    {
        _title = title;
        _message = message;

        // Initialize a timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1) // Update every second
        };
        _timer.Tick += (sender, e) => GetEstimatedFinish(estimatedTime);
        _timer.Start();

        // Immediately call the method once to show the initial estimated finish time
        GetEstimatedFinish(estimatedTime);
    }
    
    private void GetEstimatedFinish(TimeSpan estimatedTime)
    {
        var finishTime = DateTime.Now.Add(estimatedTime);
        CurrentTime = finishTime.ToString("HH:mm:ss");
    }
    
    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
        Close();
        DialogWindow?.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        Confirmed = false;
        Close();
        DialogWindow?.Close();
    }
}