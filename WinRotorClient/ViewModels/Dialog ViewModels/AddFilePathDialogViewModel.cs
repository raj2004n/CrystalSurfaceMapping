using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinRotor.ViewModels;

public partial class AddFilePathDialogViewModel : DialogViewModel
{   
    [ObservableProperty] private string _title = "Add...";
    [ObservableProperty] private string _fileTitle = "";
    [ObservableProperty] private string _filePath = @"C:\";
    [ObservableProperty] private string _confirmText = "OK";
    [ObservableProperty] private string _cancelText = "Cancel";
    [ObservableProperty] private bool _confirmed;

    public Window? DialogWindow { get; set; }
    
    
    [RelayCommand]
    private void Confirm(string title)
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