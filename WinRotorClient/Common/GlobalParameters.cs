namespace WinRotor.Common;

public static class GlobalParameters
{
    public const int X = 3;
    public const int Y = 2;
    public const int Z = 1;
    
    // Note: These are never sent to Server, server assumes these are 0, 4. 
    // These are just here to be printed out in the command logs
    public const int ResponseMethod = 0;
    public const int Speed = 4;
    
    // Chamonix Limits
    public const int XLim = 20541; // pulses
    public const int YLim = 20541; // pulses
    public const int ZLim = 20541; // pulses
    
    // WinSpec Limits
    public const double AcquisitionTimeLim = 100.00; // seconds
    public const int AcquisitionNumberLim = 100;
    public const int GratingPosLim = 5000; // rel/cm
    
    // Unit Conversion Parameters
    // Used to calculate the stepsize in pulse to send to controller
    // Controller only accepts pulses and in integer values (thus, set to integer)
    // eg. of use: stepsize [pulses] = stepsize [microns] * MicronsToPulse
    public const double PulseOverMicronsY = 4;
    public const double PulseOverMicronsZ = 7;
}