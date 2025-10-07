namespace WinRotor.Models;

public record MapParameters(
    int InitialX,
    int InitialY,
    int InitialZ,
    int GridPointsX, 
    int GridPointsY, 
    int CurrentGridPos,
    double StepSizeX, 
    double StepSizeY,
    double ProgressBar,
    string SaveName,
    string FilePath, 
    bool Reset
    );