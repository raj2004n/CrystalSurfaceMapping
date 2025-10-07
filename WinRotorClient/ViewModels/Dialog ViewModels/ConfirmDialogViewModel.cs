using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinRotor.ViewModels;

public partial class ConfirmDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = "Confirm";
    [ObservableProperty] private string _message = "Confirm message string.";
    [ObservableProperty] private string _confirmText = "OK";
    [ObservableProperty] private string _cancelText = "Cancel";
    [ObservableProperty] private bool _confirmed;

    // Direct reference to the window for immediate closure
    public Window? DialogWindow { get; set; }

    public ConfirmDialogViewModel(string title, string message)
    {
        _title = title;
        _message = message;
    }

    public ConfirmDialogViewModel()
    {
        
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