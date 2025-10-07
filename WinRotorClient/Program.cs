using Avalonia;
using System;
using System.Diagnostics;
using WinRotor.Services;

namespace WinRotor;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                EnableMultiTouch = false,
                OverlayPopups = true, // Important for transparency
                EnableIme = true,
                UseDBusMenu = true
            })
            .WithInterFont()
            .LogToTrace();
}