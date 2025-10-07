using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinRotor.Data;
using WinRotor.Factories;
using WinRotor.Models.Clients;
using WinRotor.Services;

namespace WinRotor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    private readonly StateService _stateService;    
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HomeIsActive))]
    [NotifyPropertyChangedFor(nameof(MapIsActive))]
    [NotifyPropertyChangedFor(nameof(ControlPanelIsActive))]
    [NotifyPropertyChangedFor(nameof(HelpIsActive))]
    
    
    private PageViewModel _currentPage = null!;
    public bool HomeIsActive => CurrentPage.PageName == ApplicationPageNames.Home;
    public bool MapIsActive => CurrentPage.PageName == ApplicationPageNames.Map;
    public bool ControlPanelIsActive => CurrentPage.PageName == ApplicationPageNames.ControlPanel;
    public bool HelpIsActive => CurrentPage.PageName ==  ApplicationPageNames.Help;
    
    public MainViewModel(PageFactory pageFactory, StateService stateService)
    {
        _pageFactory = pageFactory;
        _stateService = stateService;
        _stateService.ServerStateChanged += OnServerStateChanged;
        GoToHome();
    }
    
    private void OnServerStateChanged()
    {
        GoToHomeCommand.NotifyCanExecuteChanged();
        GoToMapCommand.NotifyCanExecuteChanged();
        GoToControlPanelCommand.NotifyCanExecuteChanged();
        GoToHelpCommand.NotifyCanExecuteChanged();
    }
    
    private bool CanExecuteNavigation() => !_stateService.IsWaitingForServer;
    
    [RelayCommand(CanExecute = nameof(CanExecuteNavigation))]
    private void GoToHome() => CurrentPage = _pageFactory.GetPageViewModel<HomeViewModel>();
    [RelayCommand(CanExecute = nameof(CanExecuteNavigation))]
    private void GoToMap() => CurrentPage = _pageFactory.GetPageViewModel<MapViewModel>();
    [RelayCommand(CanExecute = nameof(CanExecuteNavigation))]
    private void GoToControlPanel() => CurrentPage = _pageFactory.GetPageViewModel<ControlPanelViewModel>();
    [RelayCommand(CanExecute = nameof(CanExecuteNavigation))]
    private void GoToHelp() => CurrentPage = _pageFactory.GetPageViewModel<HelpViewModel>();
    
}