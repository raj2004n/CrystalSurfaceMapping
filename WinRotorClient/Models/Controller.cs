using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinRotor.Common;
using WinRotor.Models.Clients;

namespace WinRotor.Models;

public class Controller
{
    private readonly RotorClient _rotorClient;
    private readonly WinSpecClient _winSpecClient;

    public Controller(RotorClient rotorClient, WinSpecClient winSpecClient)
    {
        _winSpecClient = winSpecClient;
        _rotorClient = rotorClient;
    }
    
    public async Task<MapParameters> Map(
        int initialX,
        int initialY,
        int initialZ,
        int gridPointsX, 
        int gridPointsY, 
        int currentGridPos,
        double stepSizeX, 
        double stepSizeY,
        double progressBarValue,
        string saveName,
        string filePath,
        bool reset,
        Action<double> addProgress,
        Action<string> currentAction,
        Action<string> cmdCallback,
        Action<string> descCallback,
        Action<(string, string)> popupCallback,
        CancellationToken ct)
    {
        string rotorResponse;
        // Get progress per step (for progress bar)
        var progressPerStep = GetMapProgressPerStep(gridPointsX, gridPointsY);

        // Initialise current progress bar value
        var currentProgressBar = progressBarValue;
        
        // If the last Mapping was finished, then go to starting grid
        if (reset)
        {
            double stepToFirstGridX;
            double stepToFirstGridY;

            // Determine microns to move by to reach grid spot 1 
            if (gridPointsX % 2 == 0) // if even
            {
                stepToFirstGridX = -stepSizeX * ((double)gridPointsX / 2);
            }
            else
            {
                stepToFirstGridX = -stepSizeX * (((double)gridPointsX - 1) / 2);
            }

            if (gridPointsY % 2 == 0) // if even
            {
                stepToFirstGridY = -stepSizeY * ((double)gridPointsY / 2);
            }
            else
            {
                stepToFirstGridY = -stepSizeY * (((double)gridPointsY - 1) / 2);
            }

            try
            {
                descCallback("Mapping process started.");
            
                // Update current action and command log
                currentAction("Moving to Grid 1");
                
                // Move to initial Z position
                rotorResponse = await _rotorClient.Rps(GlobalParameters.Z, stepToFirstGridY);
                cmdCallback($"SENT: RPS{GlobalParameters.Z}/{GlobalParameters.Speed}/{stepToFirstGridY}/{GlobalParameters.ResponseMethod}");
                
                // If fails to move
                if (rotorResponse != "Ok")
                {
                    popupCallback(("Failed", $"{rotorResponse}"));
                    return new MapParameters(
                        initialX,
                        initialY,
                        initialZ,
                        gridPointsX,
                        gridPointsY,
                        CurrentGridPos: 0,
                        stepSizeX,
                        stepSizeY,
                        currentProgressBar,
                        saveName,
                        filePath,
                        Reset : true);
                }
                
                // Update progress for moving
                addProgress(progressPerStep);
                currentProgressBar += progressPerStep;
                
                // Move to initial Y position
                rotorResponse = await _rotorClient.Rps(GlobalParameters.Y, stepToFirstGridX);
                
                cmdCallback($"SENT: RPS{GlobalParameters.Y}/{GlobalParameters.Speed}/{stepToFirstGridX}/{GlobalParameters.ResponseMethod}"); 
                // If fails to move
                if (rotorResponse != "Ok")
                {
                    popupCallback(("Failed", $"{rotorResponse}"));
                    return new MapParameters(
                        initialX,
                        initialY,
                        initialZ,
                        gridPointsX,
                        gridPointsY,
                        CurrentGridPos: 1,
                        stepSizeX,
                        stepSizeY,
                        currentProgressBar,
                        saveName,
                        filePath,
                        Reset : true);
                }
                
                // Update progress for moving
                addProgress(progressPerStep);
                currentProgressBar += progressPerStep;
                
                currentAction("Acquiring Data at Grid 1");
                
                // Get current position
                var positionA = (
                    await _rotorClient.Rde(GlobalParameters.X), 
                    await _rotorClient.Rde(GlobalParameters.Y),
                    await _rotorClient.Rde(GlobalParameters.Z)
                    ); 
                
                // Acquire Data, set file name as the Current position
                await _winSpecClient.SendWinSpecCommand("ACQUIRE_DATA", $"{filePath}\\" + $"{saveName}" + $"[1]({positionA.Item1}, {positionA.Item2}, {positionA.Item3})");
                
                // Update description log
                descCallback($"Acquired Data at Grid 1 at Position: ({positionA.Item1}, {positionA.Item2}, {positionA.Item3})");
                
                // Update progress for acquiring data
                addProgress(progressPerStep);
                currentProgressBar += progressPerStep;
            }
            catch (Exception ex)
            {
                // Update current action and logs
                currentAction("Cancelled Mapping Procedure");
                descCallback($"Failed mapping {gridPointsX * gridPointsY} points.");
                popupCallback(("Failed", $"Error occured while mapping: {ex.Message}"));
            
                // Return current progress
                return new MapParameters(
                    initialX,
                    initialY,
                    initialZ,
                    gridPointsX,
                    gridPointsY,
                    currentGridPos,
                    stepSizeX,
                    stepSizeY,
                    currentProgressBar,
                    saveName,
                    filePath,
                    Reset : true);
            }
        }
        
        try
        {
            // Calculate the total number of grid points to map over
            int gridPoints = gridPointsX * gridPointsY;
            
            // Map each grid position starting from current grid position
            for (var gridPos = currentGridPos; gridPos < gridPoints; gridPos++) // loop over each grid from 1 -> fin.
            {
                // If Mapping is Paused
                if (ct.IsCancellationRequested)
                {
                    return new MapParameters(
                        initialX,
                        initialY,
                        initialZ,
                        gridPointsX,
                        gridPointsY,
                        gridPos,
                        stepSizeX,
                        stepSizeY,
                        currentProgressBar,
                        saveName,
                        filePath,
                        Reset : false);
                }
                
                // Update current action
                currentAction($"Moving To Grid {gridPos + 1}");
                // Check if at the end of the Xth row
                if (gridPos % gridPointsX == 0) 
                {
                    // Send Move command
                    rotorResponse = await _rotorClient.Rps(GlobalParameters.Z, stepSizeY);
                    
                    // Update command log
                    cmdCallback($"SENT: RPS{GlobalParameters.Z}/{GlobalParameters.Speed}/{stepSizeY}/{GlobalParameters.ResponseMethod}");
                    
                    // If fails then abort mapping
                    if (rotorResponse != "Ok")
                    {
                        popupCallback(("Failed", $"{rotorResponse}"));
                        // Return current progress - so that the user knows where it failed
                        return new MapParameters(
                            initialX,
                            initialY,
                            initialZ,
                            gridPointsX,
                            gridPointsY,
                            gridPos,
                            stepSizeX,
                            stepSizeY,
                            currentProgressBar,
                            saveName,
                            filePath,
                            Reset : true);
                    }
                    
                    // Invert the sign of step in X (when at the end of Xth row) to go flip directions
                    stepSizeX = -stepSizeX; 
                }
                else // Otherwise 
                {
                    // Send Move command
                    rotorResponse = await _rotorClient.Rps(GlobalParameters.Y, stepSizeX);
                    
                    // Update command log
                    cmdCallback($"SENT: RPS{GlobalParameters.Y}/{GlobalParameters.Speed}/{stepSizeX}/{GlobalParameters.ResponseMethod}");
                    
                    // If fails then abort mapping
                    if (rotorResponse != "Ok")
                    {
                        popupCallback(("Failed", $"{rotorResponse}"));
                        // Return current progress - so that the user knows where it failed
                        return new MapParameters(
                            initialX,
                            initialY,
                            initialZ,
                            gridPointsX,
                            gridPointsY,
                            gridPos,
                            stepSizeX,
                            stepSizeY,
                            currentProgressBar,
                            saveName,
                            filePath,
                            Reset : true);
                    }
                }
                
                // Update progress for moving 
                addProgress(progressPerStep);
                currentProgressBar += progressPerStep;
                
                // Update current action
                currentAction($"Acquiring Data at Grid {gridPos + 1}");
                
                // Get position
                var positionB = (
                    await _rotorClient.Rde(GlobalParameters.X),
                    await _rotorClient.Rde(GlobalParameters.Y), 
                    await _rotorClient.Rde(GlobalParameters.Z)
                    );
                
                // Acquire Data, set file name as the Current position
                await _winSpecClient.SendWinSpecCommand("ACQUIRE_DATA",$"{filePath}\\" + $"{saveName}" + $"[{gridPos + 1}]({positionB.Item1}, {positionB.Item2}, {positionB.Item3})");
                
                // Update description log
                descCallback($"Acquired Data at Grid {gridPos + 1} at Position: ({positionB.Item1}, {positionB.Item2}, {positionB.Item3})");
                
                // Update progress for acquiring data
                addProgress(progressPerStep);
                currentProgressBar += progressPerStep;
                
                // Update currentGridPos
                currentGridPos = gridPos;
            }
            
            // Finished Mapping
            // Update current action and logs
            currentAction("Completed");
            descCallback($"Finished Mapping {gridPointsX * gridPointsY} points.");
            popupCallback(("Completed", $"Finished Mapping {gridPointsX * gridPointsY} points."));
            
            // Return progress
            return new MapParameters(
                initialX,
                initialY,
                initialZ,
                gridPointsX,
                gridPointsY,
                currentGridPos,
                stepSizeX,
                stepSizeY,
                currentProgressBar,
                saveName,
                filePath,
                Reset : true);
        }
        catch (Exception ex)
        {
            // Update current action and logs
            currentAction("Cancelled Mapping Procedure");
            descCallback($"Failed mapping {gridPointsX * gridPointsY} points.");
            popupCallback(("Failed", $"Error occured while mapping: {ex.Message}"));
            
            // Return current progress
            return new MapParameters(
                initialX,
                initialY,
                initialZ,
                gridPointsX,
                gridPointsY,
                currentGridPos,
                stepSizeX,
                stepSizeY,
                currentProgressBar,
                saveName,
                filePath,
                Reset : true);
        }
    }

    public async Task<bool> MoveToCenter(int initialX, int initialY, int initialZ, 
        Action<string> currentAction,
        Action<string> cmdCallback,
        Action<string> descCallback,
        Action<(string, string)> popupCallback)
    {
        currentAction("Moving To Start");
        if (initialX != await _rotorClient.Rdp(GlobalParameters.X) ||
            (initialY != await _rotorClient.Rdp(GlobalParameters.Y)) ||
            (initialZ != await _rotorClient.Rdp(GlobalParameters.Z)))
        {
            try
            {
                await _rotorClient.Mps3(initialX, initialY, initialZ, 0);
                cmdCallback($"SENT: MPS{GlobalParameters.X}/{initialX}/Y/{initialY}/Z/{initialZ}");
                descCallback("Moved to center of sample");
                return true;
            }
            catch (Exception ex)
            {
                popupCallback(("Failed", $"Error Occured during moving to start: {ex.Message}"));
                return false;
            }
        }

        return true;
    }
    
    public async Task<bool> SetParameters(
        double acquisitionTime, 
        int acquisitionNumber, 
        int gratingTypeIndex, 
        double gratingPos,
        Action<string> currentAction,
        Action<string> descCallback,
        Action<(string, string)> popupCallback)
    {
        // -- SET WINSPEC PARAMETERS --
        
            currentAction("Setting WinSpec Parameters");
            // Set acquisition time
            var response = await _winSpecClient.SendWinSpecCommand("SET_ACQUISITION_TIME",acquisitionTime.ToString(CultureInfo.InvariantCulture));
            if (response != "ACQUISITION TIME SET")
            {
                popupCallback(("Failed", $"Failed to set Acquisition Time."));
                return false;
            }
            descCallback("Acquisition time set");
            
            // Set acquisition number
            response = await _winSpecClient.SendWinSpecCommand("SET_ACQUISITION_NUMBER", acquisitionNumber.ToString());
            if (response != "ACQUISITION NUMBER SET")
            {
                popupCallback(("Failed", $"Failed to set Acquisition Number.\r\n" + $"{response}"));
                return false;
            }
            descCallback("Acquisition number set");
            
            // Set grating type
            currentAction("Setting Grating");
            
            response = await _winSpecClient.SendWinSpecCommand("SET_GRATING", gratingTypeIndex.ToString());
            if (response != "GRATING SET")
            {
                popupCallback(("Failed", $"Error: \r\n" + $"{response}"));
                return false;
            }
            descCallback("Grating set");
            
            // Set grating position
            currentAction("Setting Grating Position");
            
            response = await _winSpecClient.SendWinSpecCommand("SET_GRATING_POS", gratingPos.ToString(CultureInfo.InvariantCulture));
            if (response != "GRATING POS SET")
            {
                popupCallback(("Failed", $"Error: \r\n" + $"{response}"));
                return false;
            }
            descCallback("Grating position set");
            
            return true;
    }

    public async Task<bool> ValidateInputs(
        Action<(string, string)> popupCallback,
        CancellationToken ct = default,
        int initialX = 1,
        int initialY = 1,
        int initialZ = 1,
        int gridPointsX = 1,
        int gridPointsY = 1,
        double stepsizeX = 1,
        double stepsizeY = 1,
        double acquisitionTime = 1,
        int acquisitionNumber = 1,
        int gratingPos = 1,
        string saveName = "optional",
        string filePath = "optional"
    )
    {
        // Validate Signs
        if (gridPointsX < 0 || gridPointsY < 0 ||
            stepsizeX < 0 || stepsizeY < 0 ||
            acquisitionTime < 0 || acquisitionNumber < 0 ||
            gratingPos < 0)
        {
            popupCallback(("Error", "All values other than center position must be positive."));
            return false;
        }

        // Validate Center Position range
        if (Math.Abs(initialX) > GlobalParameters.XLim ||
            Math.Abs(initialY) > GlobalParameters.YLim ||
            Math.Abs(initialZ) > GlobalParameters.ZLim)
        {
            popupCallback(("Error", "Center Position is out of range."));
            return false;
        }

        // Validate grid points range
        if (gridPointsX < 1 || gridPointsY < 1)
        {
            popupCallback(("Error", "Grid Points cannot be 0."));
            return false;
        }

        // Validate range of Acquisition Time
        if (acquisitionTime > GlobalParameters.AcquisitionTimeLim)
        {
            popupCallback(("Error", "Acquisition Time is out of range."));
            return false;
        }

        // Validate range of Acquisition Number
        if (acquisitionNumber > GlobalParameters.AcquisitionNumberLim)
        {
            popupCallback(("Error", "Acquisition Number is out of range."));
            return false;
        }

        // Validate range of Grating Pos
        if (gratingPos > GlobalParameters.GratingPosLim)
        {
            popupCallback(("Error", "Grating Position is out of range."));
            return false;
        }

        // Validate Save Name
        if (string.IsNullOrWhiteSpace(saveName))
        {
            popupCallback(("Error", "Save name is empty."));
            return false;
        }

        if (HasInvalidFileNameChars(saveName))
        {
            popupCallback(("Error",
                "Save name contains invalid characters. Invalid characters are: \\ / : * ? \" < > |"));
            return false;
        }

        // If file path is optional, no need for further checks
        if (filePath == "optional") return true;
        
        // Validate File path
        if (string.IsNullOrWhiteSpace(filePath))
        {
            popupCallback(("Error", "File path cannot be empty."));
            return false;
        }

        if (HasInvalidPathChars(filePath))
        {
            popupCallback(("Error", "File path contains invalid characters."));
            return false;
        }
        
        // If the requested directory does not exist, attempt to make a new one
        if (await CheckDirectoryExists(filePath, ct)) return true;
        
        // Try to create a new directory
        var createResponse = await CreateDirectory(filePath);

        if (createResponse == "DIRECTORY CREATED")
        {
            popupCallback(("Info", "The directory did not exist. A new directory was successfully created."));
        }
        else // If fails to create, abort StartMap
        {
            // Dialog to display error message
            popupCallback(("Error", "The directory did not exist. A new directory could not created."));
            return false;
        }

        // Passes all checks
        return true;
    }

    public async Task<bool> CheckRange(
        int gridPointsY, 
        int gridPointsZ, 
        double stepSizeY, 
        double stepSizeZ, 
        Action<(string, string)> popupCallback)
    {
        // Get current position in pulses
        var position = await _rotorClient.GetXyz();
        
        // Invert direction of Z value to align with axis
        stepSizeZ *= -1;
        
        // Calculate free distance needed in microns
        double disY;
        double disZ;
        
        if (gridPointsY % 2 == 0)
        {
            disY = gridPointsY * stepSizeY / 2;
        }
        else
        {
            disY = (gridPointsY - 1) * stepSizeY / 2;
        }
        
        if (gridPointsZ % 2 == 0)
        {
            disZ = gridPointsZ * stepSizeZ / 2;
        }
        else
        {
            disZ = (gridPointsZ - 1) * stepSizeZ / 2;
        }
        
        // Convert distance to pulses
        disY *= GlobalParameters.PulseOverMicronsY;
        disZ *= GlobalParameters.PulseOverMicronsZ;
        
        // Test if distance within range
        if (position.yPos + disY > GlobalParameters.YLim || position.yPos - disY < -GlobalParameters.YLim)
        {
            popupCallback(("Error", "Mapping can not proceed due to the lack of range for Y."));
            return false;
        }

        if (position.zPos + disZ > GlobalParameters.ZLim || position.zPos - disZ < -GlobalParameters.ZLim)
        {
            popupCallback(("Error", "Mapping can not proceed due to the lack of range for Z."));
            return false;
        }
        
        return true;
    }
    
    private bool HasInvalidFileNameChars(string fileName)
    {
        // Get an array of characters that are not allowed in filenames
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return fileName.Any(c => invalidChars.Contains(c));
    }

    private bool HasInvalidPathChars(string filePath)
    {
        // Get an array of characters that are not allowed in paths
        char[] invalidChars = Path.GetInvalidPathChars();
        return filePath.Any(c => invalidChars.Contains(c));
    }
    
    public TimeSpan CalculateEstimatedTime(int gridPointsX, int gridPointsY, int acquisitionNumber, double acquisitionTime)
    {
        // Calculate the total estimated time in seconds
        double totalSeconds = gridPointsX * gridPointsY * acquisitionNumber * acquisitionTime;

        // Return a TimeSpan object from the total number of seconds
        return TimeSpan.FromSeconds(totalSeconds);
    }

    public string CalculateEstimatedFinish(TimeSpan estimatedTime)
    {
        
        var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        
        // Get current time and convert to UK time
        var currentTimeUk = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ukTimeZone);
        
        // Add estimated finish time
        var estimatedFinishUk = currentTimeUk.AddSeconds(estimatedTime.TotalSeconds);

        // Format the result to "hh:mm:ss".
        return estimatedFinishUk.ToString(@"hh\:mm\:ss");
    }

    private double GetMapProgressPerStep(int gridPointsX, int gridPointsY)
    {
        // + 3 accounts move to y start, x start, read data
        // (gridPointsX * gridPointsY - 1) accounts first grid point is counted
        // * 2 accounts gathering data
        var totalMappingSteps = 3.0 + (gridPointsX * gridPointsY - 1.0) * 2.0;
        return 100 / (totalMappingSteps);
    }

    public async Task<bool> CheckDirectoryExists(string filePath, CancellationToken ct)
    {
        // Check for cancellation token before starting
        ct.ThrowIfCancellationRequested();
        
        // Connect to winspec
        await _winSpecClient.Connect(ct);
        
        // Check for cancellation after connecting
        ct.ThrowIfCancellationRequested();
        
        var responseTask = _winSpecClient.SendWinSpecCommand("CHECK_DIRECTORY", filePath);
        
        var response = await responseTask.WaitAsync(ct);
        
        _winSpecClient.Dispose();
        
        // This actually does not catch the error message returned if fails. But should be okay
        return response == "TRUE";
    }

    public async Task<string> CreateDirectory(string filePath)
    {
        await _winSpecClient.Connect();
        var response = await _winSpecClient.SendWinSpecCommand("CREATE_DIRECTORY", filePath);
        _winSpecClient.Dispose();

        return response;
    }
    
    public async Task ConnectToWinSpec()
    {
        await _winSpecClient.Connect();
    }
    
    public void Dispose()
    {
        _winSpecClient.Dispose();
    }
}