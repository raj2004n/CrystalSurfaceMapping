using CommunityToolkit.Mvvm.ComponentModel;
using WinRotor.Data;

namespace WinRotor.ViewModels;

public partial class PageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ApplicationPageNames _pageName;
}