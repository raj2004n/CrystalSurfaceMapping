using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinRotor.Common;
using WinRotor.Data;
using WinRotor.Models;
using WinRotor.Models.Clients;
using WinRotor.Services;
using WinRotor.Services.Dialog;

namespace WinRotor.ViewModels;

public partial class ControlPanelViewModel : PageViewModel
{
    // Display position
    [ObservableProperty] private string _xPos;
    [ObservableProperty] private string _yPos;
    [ObservableProperty] private string _zPos;
    [ObservableProperty] private string _xPosE;
    [ObservableProperty] private string _yPosE;
    [ObservableProperty] private string _zPosE;

    // Remote Control Panel
    [ObservableProperty] private string _axis = "1";
    [ObservableProperty] private string _stepSize = "10";

    [ObservableProperty] private string _goToX = "0";
    [ObservableProperty] private string _goToY = "0";
    [ObservableProperty] private string _goToZ = "0";
    
    // Clients
    private readonly RotorClient _rotorClient;
    private readonly Controller _ctrl;
    
    // Service
    private readonly IDialogService _dialogService;
    private readonly StateService _stateService;
    
    private bool IsWaitingForServer
    {
        get => _stateService.IsWaitingForServer;
        set => _stateService.IsWaitingForServer = value;
    }
    
    public ControlPanelViewModel(
        IDialogService dialogService,
        StateService stateService,
        RotorClient rotorClient, 
        WinSpecClient winSpecClient)
    {
        PageName = ApplicationPageNames.ControlPanel;
        _dialogService = dialogService;
        _rotorClient = rotorClient;
        _ctrl = new Controller(rotorClient, winSpecClient);
        _stateService = stateService;
        
        // Initialize positions while it loads
        XPos = "Loading...";
        YPos = "Loading...";
        ZPos = "Loading...";
        XPosE = "Loading...";
        YPosE = "Loading...";
        ZPosE = "Loading...";

        // Load Current positions on page launch
        _ = LoadCurrentPosition();
    }
    
    private bool CanExecuteFunction()
    {
        return !IsWaitingForServer;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFunction))]
    private async Task LoadCurrentPosition()
    {
        try
        {
            // Timeout for client call
            var getPositionEncoder = _rotorClient.GetXyzE();
            var timeoutTask = Task.Delay(3000);

            var completedTaskEncoder = await Task.WhenAny(getPositionEncoder, timeoutTask);

            if (completedTaskEncoder == timeoutTask)
            {
                // If not response within time out
                XPos = "Error";
                YPos = "Error";
                ZPos = "Error";
                XPosE = "Error";
                YPosE = "Error";
                ZPosE = "Error";

                await _dialogService.ShowInfoDialog("Error", "Not connected to the Rotor Server.");
            }
            else
            {
                // If timeout does not occur, then we are connected and should be able to get pulse values as well
                var positions = await getPositionEncoder;
                var positionsEncoder = await _rotorClient.GetXyz();
                XPos = $"{positionsEncoder.xPos}";
                YPos = $"{positionsEncoder.yPos}";
                ZPos = $"{positionsEncoder.zPos}";
                XPosE = $"{positions.xPosE}";
                YPosE = $"{positions.yPosE}";
                ZPosE = $"{positions.zPosE}";
            }
        }
        catch (Exception ex)
        {
            XPos = "Error";
            YPos = "Error";
            ZPos = "Error";
            XPosE = "Error";
            YPosE = "Error";
            ZPosE = "Error";

            await _dialogService.ShowInfoDialog("Unexpected Error", $"Could not load initial positions: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFunction))]
    private async Task MoveInDirection(string direction)
    {
        // Validate input type
        if (!double.TryParse(StepSize, out var stepSize))
        {
            await _dialogService.ShowInfoDialog("Error", "Ensure all input types are correct.");
            return;
        }
        
        // Validate input properties
        var validationPassed = await _ctrl.ValidateInputs(
            popUp => { _dialogService.ShowInfoDialog(popUp.Item1, popUp.Item2); },
            stepsizeX: stepSize,
            stepsizeY: stepSize
        );
        
        // Abort if validation fails
        if (!validationPassed)
        {
            return;
        }
        try
        {
            IsWaitingForServer = true;
            
            // Initialise a temporary task as back in cases where case is not hit
            Task moveTask = _rotorClient.Rps(GlobalParameters.X, 0);
            
            switch (direction)
            {
                case "in":
                    moveTask = _rotorClient.Rps(GlobalParameters.X, -stepSize);
                    break;
                case "out":
                    moveTask = _rotorClient.Rps(GlobalParameters.X, stepSize);
                    break;
                case "up":
                    moveTask = _rotorClient.Rps(GlobalParameters.Z, stepSize);
                    break;
                case "down":
                    moveTask = _rotorClient.Rps(GlobalParameters.Z, -stepSize);
                    break;
                case "left":
                    moveTask = _rotorClient.Rps(GlobalParameters.Y, -stepSize);
                    break;
                case "right":
                    moveTask = _rotorClient.Rps(GlobalParameters.Y, stepSize);
                    break;
                case "upLeft":
                    moveTask = _rotorClient.Mps2(-stepSize, stepSize, 1);
                    break;
                case "upRight":
                    moveTask = _rotorClient.Mps2(stepSize, stepSize, 1);
                    break;
                case "downLeft":
                    moveTask = _rotorClient.Mps2(-stepSize, -stepSize, 1);
                    break;
                case "downRight":
                    moveTask = _rotorClient.Mps2(stepSize, -stepSize, 1);
                    break;
            }
            
            // Create a timeout task
            var timeoutTask = Task.Delay(5000);

            // Wait for either the server task or the timeout task to complete
            var completedTask = await Task.WhenAny(moveTask, timeoutTask);

            // Check which task completed first
            if (completedTask == timeoutTask)
            {
                // The timeout occurred
                await _dialogService.ShowInfoDialog("Error", "The rotors took too long to respond and the operation was aborted.");
            }
            else
            {
                await moveTask;
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowInfoDialog("Error",
                $"Could not move the rotors: \r\n" +
                $"{ex.Message}");
        }
        finally
        {
            IsWaitingForServer = false;
            await LoadCurrentPosition();
        }
        
    }
    
    [RelayCommand(CanExecute = nameof(CanExecuteFunction))]
    private async Task GoTo()
    {
        if (!int.TryParse(GoToX, out var posX) ||
            !int.TryParse(GoToY, out var posY) ||
            !int.TryParse(GoToZ, out var posZ))
        {
            await _dialogService.ShowInfoDialog("Error",
                "Invalid input values. Please enter numbers for all fields.");
            return;
        }

        if (Math.Abs(posX) > GlobalParameters.XLim || 
            Math.Abs(posY) > GlobalParameters.YLim ||
            Math.Abs(posZ) > GlobalParameters.ZLim)
        {
            await _dialogService.ShowInfoDialog("Error",
                "Ensure position are within range.");
            return;
        }

        try
        {
            IsWaitingForServer = true;
            // drivingType = 0 -> absolute position
            await _rotorClient.Mps3(posX, posY, posZ, 0);
            await LoadCurrentPosition();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowInfoDialog("Position Command Failed",
                $"Could not move the rotors: \r\n" +
                $"{ex.Message}");
        }
        finally
        {
            IsWaitingForServer = false;
        }

    }

    [RelayCommand(CanExecute = nameof(CanExecuteFunction))]
    private async Task GoToOrigin()
    {
        try
        {
            IsWaitingForServer = true;
            // drivingType = 0 -> absolute position
            await _rotorClient.Mps3(0, 0, 0, 0);
            await LoadCurrentPosition();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowInfoDialog("Position Command Failed",
                $"Could not move the rotors: \r\n" +
                $"{ex.Message}");
        }
        finally
        {
            IsWaitingForServer = false;
        }
    }

}