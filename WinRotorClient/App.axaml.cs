using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WinRotor.Data;
using WinRotor.Factories;
using WinRotor.Models.Clients;
using WinRotor.Services;
using WinRotor.Services.Dialog;
using WinRotor.ViewModels;
using WinRotor.Views;

namespace WinRotor;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(new ViewLocator());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection(); // create an empty container
        collection.AddSingleton<MainViewModel>();// add dependencies...
        
        // Service
        collection.AddSingleton<IDialogService, DialogService>();
        collection.AddSingleton<StateService>();
        
        // Clients
        collection.AddSingleton<RotorClient>(sp => new RotorClient("192.168.1.2", 5000));
        collection.AddSingleton<WinSpecClient>(sp => new WinSpecClient("192.168.1.1", 5000));
        
        // Pages
        collection.AddTransient<HomeViewModel>();
        collection.AddSingleton<MapViewModel>();
        collection.AddTransient<ControlPanelViewModel>();
        collection.AddTransient<HelpViewModel>();
        collection.AddSingleton<Func<Type, PageViewModel>>(x => type => type switch
        {
            _ when type == typeof(HomeViewModel) => x.GetRequiredService<HomeViewModel>(),
            _ when type == typeof(MapViewModel) => x.GetRequiredService<MapViewModel>(),
            _ when type == typeof(ControlPanelViewModel) => x.GetRequiredService<ControlPanelViewModel>(),
            _ when type == typeof(HelpViewModel) => x.GetRequiredService<HelpViewModel>(),
            _ => throw new InvalidOperationException($"Page of type {type?.FullName} has no view model"),
        });
        collection.AddSingleton<PageFactory>();
        
        // TopLevel provider
        collection.AddSingleton<Func<TopLevel?>>(x => () =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime topWindow)
                return TopLevel.GetTopLevel(topWindow.MainWindow);
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
                return TopLevel.GetTopLevel(singleViewPlatform.MainView);

            return null;
        });
        
        var services = collection.BuildServiceProvider(); // finalize container
        
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainView
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}