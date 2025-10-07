using System;

namespace WinRotor.Services;

public class StateService
{
    public event Action? ServerStateChanged;

    private bool _isWaitingForServer;

    public bool IsWaitingForServer
    {
        get => _isWaitingForServer;
        set
        {
            if (_isWaitingForServer != value)
            {
                _isWaitingForServer = value;
                ServerStateChanged?.Invoke();
            }
        }
    }
}