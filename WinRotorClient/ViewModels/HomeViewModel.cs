using System;
using System.Text.Unicode;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinRotor.Data;
using WinRotor.Models.Clients;
using WinRotor.Services;

namespace WinRotor.ViewModels;

public partial class HomeViewModel : PageViewModel
{ 
    private readonly RotorClient _rotorClient;
    private readonly WinSpecClient _winSpecClient;
    
    [ObservableProperty] private bool _rotorConnectionStatus;
    [ObservableProperty] private bool _winSpecConnectionStatus;
    [ObservableProperty] private string _lastCheckedRotor = "--- --:--:--";
    [ObservableProperty] private string _lastCheckedWinSpec = "--- --:--:--";
    
    private readonly StateService _stateService;

    private bool IsWaitingForServer
    {
        get => _stateService.IsWaitingForServer;
        set => _stateService.IsWaitingForServer = value;
    }
    
    public HomeViewModel(RotorClient rotorClient, WinSpecClient winSpecClient, StateService stateService)
    {
        PageName = ApplicationPageNames.Home;
        _rotorClient = rotorClient;
        _winSpecClient = winSpecClient;
        _stateService = stateService;
        
        // Test for connection at load
        _ = TestRotorConnection();
        _ = TestWinSpecConnection();
    }
    
    
    [RelayCommand]
    private async Task TestRotorConnection()
    {
        LastCheckedRotor = DateTime.Now.ToString("dddd hh:mm tt");
        try
        {
            IsWaitingForServer = true;
            var connection = _rotorClient.TestConnection();
            var timeoutTask = Task.Delay(3000);

            var completedTask = await Task.WhenAny(timeoutTask, connection);
            if (completedTask == timeoutTask)
            {
                RotorConnectionStatus = false;
                Console.WriteLine("No Rotor connection.");
            }
            else
            {
                await connection;
                RotorConnectionStatus = true;
            }
        }
        catch (Exception ex)
        {
            RotorConnectionStatus = false;
            Console.WriteLine($"No Rotor connection: {ex.Message}");
        }
        finally
        {
            IsWaitingForServer = false;
        }
    }
    
    [RelayCommand]
    private async Task TestWinSpecConnection()
    {
        LastCheckedWinSpec = DateTime.Now.ToString("dddd hh:mm tt");
        try
        {
            IsWaitingForServer = true;
            var connection = _winSpecClient.Connect();
            var timeoutTask = Task.Delay(3000);

            var completedTask = await Task.WhenAny(timeoutTask, connection);
            if (completedTask == timeoutTask)
            {
                WinSpecConnectionStatus = false;
                Console.WriteLine("No WinSpec connection.");
            }
            else
            {
                await connection;
                WinSpecConnectionStatus = true;
                // Disconnect after ensuring connection can be established
                await _winSpecClient.Disconnect();
            }
        }
        catch (Exception ex)
        {
            WinSpecConnectionStatus = false;
            Console.WriteLine($"No WinSpec connection: {ex.Message}");
        }
        finally
        {
            IsWaitingForServer = false;
        }
    }
}