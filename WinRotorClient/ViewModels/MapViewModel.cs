using System;
using System.Collections.Generic;
using WinRotor.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using WinRotor.Models;
using WinRotor.Models.Clients;
using WinRotor.Services;
using WinRotor.Services.Logging;

namespace WinRotor.ViewModels;

public partial class MapViewModel : PageViewModel
{
    private readonly Controller _ctrl;
    private readonly RotorClient _rotorClient;
    private readonly Logger _logger;
    private readonly IDialogService _dialogService;
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _displayTimer;
    private readonly StateService _stateService;
    private ObservableCollection<FileItem> FileItems { get; } = [];
    private List<FileItem>? _filePaths = [];

    // Path to your data file
    private readonly string _dataFilePath;
    
    #region Input Parameters

    // Rotor Parameters
    [ObservableProperty] private string _initialX = "Loading...";
    [ObservableProperty] private string _initialY = "Loading...";
    [ObservableProperty] private string _initialZ = "Loading...";
    [ObservableProperty] private string _gridPointsX = "3";
    [ObservableProperty] private string _gridPointsY = "3";
    [ObservableProperty] private string _stepsizeX = "10";
    [ObservableProperty] private string _stepsizeY = "10";
    [ObservableProperty] private string _speed = "4";
    
    public string[] Gratings { get; } = ["150 BLZ = 500NM", "1200 BLZ = 750NM", "600 BLZ = 750NM"];
    
    // WinSpec Parameters 
    [ObservableProperty] private string _acquisitionTime = "0.5";
    [ObservableProperty] private string _acquisitionNumber = "5";
    [ObservableProperty] private string _gratingType = "1200 BLZ = 750NM";
    [ObservableProperty] private string _gratingPos= "200";
    
    // Save Parameters
    [ObservableProperty] private string _filePath = @"C:\Users\DomLanLab\Desktop\Test\";
    [ObservableProperty] private string _saveName = "apple";
    
    // Status Parameters
    [ObservableProperty] private string _timeElapsed = "00:00:00";
    [ObservableProperty] private string _expectedFinish = "--:--:--";
    [ObservableProperty] private double _progressBar;
    [ObservableProperty] private string _currentAction = "Waiting for Command...";
    
    // Logging
    [ObservableProperty] private ObservableCollection<string> _cmdLog = [];
    [ObservableProperty] private ObservableCollection<string> _descLog = [];

    #endregion

    #region Saved Parameters

    private MapParameters _mapParameters = new MapParameters(
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        string.Empty,
        string.Empty, 
        true
        );

    private bool IsWaitingForServer
    {
        get => _stateService.IsWaitingForServer;
        set => _stateService.IsWaitingForServer = value;
    }

    #endregion
    
    
    public MapViewModel(
        IDialogService dialogService, 
        RotorClient rotorClient, 
        WinSpecClient winSpecClient,
        StateService stateService)
    {
        PageName = ApplicationPageNames.Map;
        
        // Set Main Controller as _ctrl
        _ctrl = new Controller(rotorClient, winSpecClient);
        
        // Set Rotor Controller 
        _rotorClient = rotorClient;
        
        _logger = new Logger(this);
        _dialogService =  dialogService;
        _stateService = stateService;
        
        // Timer
        _displayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _displayTimer.Tick += (_, _) => UpdateElapsedTime();

        
        // Current Position
        _ = LoadCurrentPosition();
        
        // Initialize the file path in the constructor
        var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var appFolder = Path.Combine(documentsFolder, "WinRotor");
    
        // Create the directory if it doesn't exist
        Directory.CreateDirectory(appFolder);
        _dataFilePath = Path.Combine(appFolder, "saved_paths.json");

        // Load existing paths when the application starts
        LoadPaths();
    }
    
    #region Mapping Commands
    private bool CanExecuteFunction()
    {
        return !IsWaitingForServer; // if map is running, then can't execute function
    }
    
    [RelayCommand(CanExecute = nameof(CanExecuteFunction))]
    private async Task StartMap(CancellationToken ct)
    {
        // Check if prior Mapping finished (aka if reset is true then mapping was finihsed)
        if (!_mapParameters.Reset) // Did not finish
        {
            // Ask user to confirm restarting the Mapping procedure
            var resetConfirm = await _dialogService.ShowConfirmDialog(
                "Confirm",
                "This will restart the Mapping procedure. If you wish to continue instead, please choose CONTINUE after closing this dialog."
            );

            if (!resetConfirm)
            {
                return;
            }
            
            // Load current position (to update initial x,y,z)
            _ = LoadCurrentPosition();
        }
 
        // Parse input
        if (!int.TryParse(InitialX, out var initialX) ||
            !int.TryParse(InitialY, out var initialY) ||
            !int.TryParse(InitialZ, out var initialZ) ||
            !int.TryParse(GridPointsX, out var gridPointsY) ||
            !int.TryParse(GridPointsY, out var gridPointsZ) ||
            !double.TryParse(StepsizeX, out var stepSizeY) ||
            !double.TryParse(StepsizeY, out var stepSizeZ) ||
            !double.TryParse(AcquisitionTime, out var acquisitionTime) ||
            !int.TryParse(AcquisitionNumber, out var acquisitionNumber) ||
            !int.TryParse(GratingPos, out var gratingPos))
        {
            await _dialogService.ShowInfoDialog("Error", "Ensure all input types are correct.");
            return;
        }
        
        // File path must end in '\' (handled inside Controller, so here just remove it)
        FilePath = FilePath.TrimEnd('\\');

        // Validate inputs
        var validationPassed = await _ctrl.ValidateInputs(
            popUp => { _dialogService.ShowInfoDialog(popUp.Item1, popUp.Item2); },
            ct,
            initialX, 
            initialY, 
            initialZ, 
            gridPointsY,  
            gridPointsZ,
            stepSizeY, 
            stepSizeZ, 
            acquisitionTime, 
            acquisitionNumber, 
            gratingPos,
            SaveName,
            FilePath
            );
        
        // Abort if validation fails
        if (!validationPassed)
        {
            return;
        }

        ProgressBar = 0;
        
        // Check if mapping is in range
        var validRange = await _ctrl.CheckRange(
            gridPointsY, 
            gridPointsZ, 
            stepSizeY, 
            stepSizeZ,
            popUp => { _dialogService.ShowInfoDialog(popUp.Item1, popUp.Item2); });

        // Abort if Mapping not possible
        if (!validRange)
        {
            return;
        }
        
        // Calculate estimated time
        var estimatedTime = _ctrl.CalculateEstimatedTime(gridPointsY, gridPointsZ, acquisitionNumber, acquisitionTime);
            
        // Show confirmation dialog. The binding for CurrentTime will now constantly update.
        var confirmed = await _dialogService.ShowConfirmMapDialog(
            "Confirm",
            $"Estimated Time to Map {gridPointsY} x {gridPointsZ} points: " + 
            $"{estimatedTime.TotalMinutes} minutes.",
            estimatedTime
        );
            
        if (!confirmed)
        {
            _logger.LogDescription("Mapping operation cancelled by user.");
            return;
        }
        
        try
        {
            // Assign current values to saved parameters (Used for pausing and continuing)
            _mapParameters = new MapParameters(
                initialX,
                initialY,
                initialZ,
                gridPointsY,
                gridPointsZ,
                1,
                stepSizeY,
                stepSizeZ,
                ProgressBar,
                SaveName,
                FilePath,
                true);

            // Get grating type index in 1 base (required by WinSpec)
            var gratingTypeIndex = Array.IndexOf(Gratings, GratingType) + 1;
            
            // Update expected finish
            ExpectedFinish = _ctrl.CalculateEstimatedFinish(estimatedTime);

            // Connect to WinSpec server
            await _ctrl.ConnectToWinSpec();
            
            // Set WinSpec and Rotor parameters
            var success = await _ctrl.SetParameters(
                acquisitionTime,
                acquisitionNumber,
                gratingTypeIndex,
                gratingPos,
                currentAction => { CurrentAction = currentAction; },
                desc => { _logger.LogDescription(desc); },
                popUp => { _dialogService.ShowInfoDialog(popUp.Item1, popUp.Item2); }
            );
            
            // Disconnect from WinSpec
            _ctrl.Dispose();
            
            if (success)
            {
                // Move to the center position
                success = await _ctrl.MoveToCenter(
                    _mapParameters.InitialX,
                    _mapParameters.InitialY,
                    _mapParameters.InitialZ,
                    currentAction => { CurrentAction = currentAction; },
                    cmd => { _logger.LogCommand(cmd); },
                    desc => { _logger.LogDescription(desc); },
                    popUp => { _dialogService.ShowInfoDialog(popUp.Item1, popUp.Item2); }
                );
            }

            if (success)
            {
                // Start mapping after preparations are done
                await MapCommand.ExecuteAsync(ct);
            }
            

        }
        catch (OperationCanceledException)
        {
            _logger.LogDescription("Mapping operation cancelled due to timeout.");
            await _dialogService.ShowInfoDialog("Timeout", "The operation timed out. Please try again.");
        }
        catch (Exception e)
        {
            _logger.LogDescription($"An error occurred during mapping: {e.Message}");
            await _dialogService.ShowInfoDialog("Failed", $"The mapping process failed due to an error: {e.Message}");
        }
    }
    
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Map(CancellationToken ct)
    {
        IsWaitingForServer = true;
        
        // Start timer
        _stopwatch.Start();
        _displayTimer.Start();

        try
        {
            await _ctrl.ConnectToWinSpec();
            // Execute Map and store map parameters (thus if the function is paused, it can be recontinued)
            _mapParameters = await _ctrl.Map(
                _mapParameters.InitialX,
                _mapParameters.InitialY,
                _mapParameters.InitialZ,
                _mapParameters.GridPointsX,
                _mapParameters.GridPointsY,
                _mapParameters.CurrentGridPos,
                _mapParameters.StepSizeX,
                _mapParameters.StepSizeY,
                _mapParameters.ProgressBar,
                _mapParameters.SaveName,
                _mapParameters.FilePath,
                _mapParameters.Reset,
                addProgress => { ProgressBar += addProgress; },
                currentAction => { CurrentAction = currentAction; },
                cmd => { _logger.LogCommand(cmd); },
                desc => { _logger.LogDescription(desc); },
                popUp => { _dialogService.ShowInfoDialog(popUp.Item1, popUp.Item2); },
                ct
            );

            // If Mapping is finished, aka when reset flag is true
            if (_mapParameters.Reset) 
            {
                // Return to center
                await _ctrl.MoveToCenter(
                    _mapParameters.InitialX,
                    _mapParameters.InitialY,
                    _mapParameters.InitialZ,
                    currentAction => { CurrentAction = currentAction; },
                    cmd => { _logger.LogCommand(cmd); },
                    desc => { _logger.LogDescription(desc); },
                    popUp => { _dialogService.ShowInfoDialog(popUp.Item1, popUp.Item2); }
                );
            }
        }
        finally
        {
            _stopwatch.Stop();
            _displayTimer.Stop();
            IsWaitingForServer = false;
            _ctrl.Dispose();
            
            if (_mapParameters.Reset)
            {
                _stopwatch.Reset();
            }
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanExecuteFunction))] // this stops continue from being executable when map is running
    private async Task Continue(CancellationToken ct)
    {
        
        // check if isFinished
        if (_mapParameters.Reset)
        {
            await _dialogService.ShowInfoDialog("Error", "Prior Mapping has already been completed.");
            return;
        }
        
        // Check if no saved progress (in case some bug deletes saved data, grid points X is a good check since user is disallowed to enter 0 for it)
        if (_mapParameters.GridPointsX == 0)
        {
            await _dialogService.ShowInfoDialog("Error", "No prior saved progress was found.");
            return; 
        }
        
        // Start mapping
        await MapCommand.ExecuteAsync(ct);
    }
    
    #endregion
    
    #region File Path Operations
    [RelayCommand]
    private async Task DisplaySavedFilePaths()
    {
        // Clear the current list
        FileItems.Clear();

        // Load the file paths from the saved JSON file
        LoadPaths(); 

        // Iterate over each loaded FileItem object
        if (_filePaths != null)
        {
            foreach (var fileItem in _filePaths)
            {
                FileItems.Add(fileItem);
            }
        }

        // Show dialog with File Path list
        var result = await _dialogService.ShowFilesListDialog(FileItems);

        // If user selects a path, set the current file path to user selected one
        if (result.Confirmed)
        {
            FilePath = result.SelectedFile.FilePath;
        }
    }
    
    [RelayCommand]
    private async Task AddExistingFilePath()
    {
        // Create a CancellationTokenSource that will cancel after 1 second.
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            // Prompt user to enter file name and file path
            var (confirmLoad, fileTitleToLoad, filePathToLoad) = await _dialogService.ShowAddFilePathDialog("Add Existing File Path");

            // If user did not confirm, return immediately.
            if (!confirmLoad)
            {
                return;
            }

            // Use the token to cancel if CheckDirectoryExists takes too long.
            if (await _ctrl.CheckDirectoryExists(filePathToLoad, cts.Token) && !FileItems.Any(f => f.FilePath.Equals(filePathToLoad, StringComparison.OrdinalIgnoreCase)))
            {
                var newItem = new FileItem
                {
                    Title = fileTitleToLoad,
                    FilePath = filePathToLoad
                };

                FileItems.Add(newItem); // Add to the ObservableCollection
    
                // Add to the underlying list for saving
                if (_filePaths != null && !_filePaths.Any(f => f.FilePath.Equals(filePathToLoad, StringComparison.OrdinalIgnoreCase)))
                {
                    _filePaths.Add(newItem); // Add the new FileItem object
                    await SavePath(); 
                }
            }
            else
            {
                await _dialogService.ShowInfoDialog("Error", "The file path does not exist or has already been added to the current saves list.");
            }
        }
        catch (OperationCanceledException)
        {
            // This exception is thrown when the token is cancelled.
            await _dialogService.ShowInfoDialog("Timeout", "The operation timed out. Please try again.");
        }
        catch (Exception ex)
        {
            // Handle other potential exceptions.
            await _dialogService.ShowInfoDialog("Error", $"An error occurred: {ex.Message}");
        }
        finally
        {
            cts.Dispose();
        }
    }
    
    [RelayCommand]
    private async Task AddNewFilePath()
    {
        // Prompt user to enter file name and file path
        var (confirmLoad, fileTitleToAdd, filePathToAdd) = await _dialogService.ShowAddFilePathDialog("Add New File Path");

        // If user did not confirm
        if (!confirmLoad)
        {
            return;
        }
    
        // Create the new file path
        var createResponse = await _ctrl.CreateDirectory(filePathToAdd);
    
        // Check if directory created
        if (createResponse == "DIRECTORY CREATED")
        {
            // Add to saves list
            var newItem = new FileItem
            {
                Title = fileTitleToAdd,
                FilePath = filePathToAdd
            };

            FileItems.Add(newItem); // Add to the ObservableCollection
    
            // Add to the underlying list for saving
            if (_filePaths != null && !_filePaths.Any(f => f.FilePath.Equals(filePathToAdd, StringComparison.OrdinalIgnoreCase)))
            {
                _filePaths.Add(newItem); // Add the new FileItem object
            
                // Await the SavePath method to ensure the file is written
                await SavePath(); 
            }
        }
        else
        {
            await _dialogService.ShowInfoDialog("Error", "The file path could not be added. Please check if the File Path is valid.");
        }
    }
    
    private async Task SavePath()
    {
        try
        {
            var jsonString = JsonSerializer.Serialize(_filePaths);
            await File.WriteAllTextAsync(_dataFilePath, jsonString);
            await _dialogService.ShowInfoDialog(
                "Saved",
                "File Path saved.");
        }
        catch (Exception ex)
        {
            // Handle potential exceptions (e.g., write permissions issues)
            Console.WriteLine($"Error saving paths: {ex.Message}");
        }
    }

    private void LoadPaths()
    {
        if (!File.Exists(_dataFilePath)) return;
    
        try
        {
            var jsonString = File.ReadAllText(_dataFilePath);
            // Deserialize into a list of FileItem objects
            _filePaths = JsonSerializer.Deserialize<List<FileItem>>(jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading paths: {ex.Message}");
            _filePaths = [];
        }
    }
    #endregion
    
    #region Additional Operations
    private void UpdateElapsedTime()
    {
        TimeElapsed = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
    }
    [RelayCommand(CanExecute = nameof(CanExecuteFunction))]
    private async Task LoadCurrentPosition()
    {
        try
        {
            // Timeout for client call
            var getPosition = _rotorClient.GetXyz();
            var timeoutTask = Task.Delay(3000);
            
            var completedTask = await Task.WhenAny(getPosition, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // If not response within time out
                InitialX = "Error";
                InitialY = "Error";
                InitialZ = "Error";

                await _dialogService.ShowInfoDialog("Error", "Not connected to the Rotor Server.");
            }
            else
            {
                var positions = await getPosition;
                InitialX = $"{positions.xPos}";
                InitialY = $"{positions.yPos}";
                InitialZ = $"{positions.zPos}"; 
            }
        }
        catch (Exception ex)
        {
            InitialX = "Error";
            InitialY = "Error";
            InitialZ = "Error";

            await _dialogService.ShowInfoDialog("Unexpected Error", $"Could not load initial positions: {ex.Message}");
        }
    }
    #endregion
}
