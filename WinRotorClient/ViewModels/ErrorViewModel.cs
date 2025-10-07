using CommunityToolkit.Mvvm.ComponentModel;

namespace WinRotor.ViewModels;

public partial class ErrorViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "Title";
    [ObservableProperty] private string _description = "Description";
}