using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinRotor.ViewModels;

public partial class InfoDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = "Info";
    [ObservableProperty] private string _message = "Information message string.";
    [ObservableProperty] private string _confirmText = "OK";
    
    public Window? DialogWindow { get; set; }
    
    public InfoDialogViewModel()
    {
    }
    
    public InfoDialogViewModel(string title, string message)
    {
        _title = title;
        _message = message;
    }
    
    [RelayCommand]
    private void Confirm()
    {
        Close();
        DialogWindow?.Close();
    }
}