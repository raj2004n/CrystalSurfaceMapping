using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinRotor.ViewModels;

public partial class DialogViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isDialogOpen;

    protected TaskCompletionSource CloseTask = new TaskCompletionSource();
    
    // Add event for window closure
    public event Action? RequestClose;
    
    public async Task WaitAsync()
    {
        await CloseTask.Task;
    }

    public void Show()
    {
        if (CloseTask.Task.IsCompleted)
            CloseTask = new TaskCompletionSource();
        IsDialogOpen = true;
    }
    
    public void Close()
    {
        IsDialogOpen = false;
        CloseTask.TrySetResult();
        RequestClose?.Invoke(); // Notify to close the window
    }
}