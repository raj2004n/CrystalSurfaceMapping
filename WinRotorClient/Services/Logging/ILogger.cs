namespace WinRotor.Services.Logging;

public interface ILogger
{
    void LogCommand(string message);
    void LogDescription(string message);
}