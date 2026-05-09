using System;
using InteractiveApp.Models;
using Splat;
using InteractiveApp.ViewModels;
using InteractiveApp.Utility;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.VisualTree;







#if IOS
using UIKit;
using Foundation;
#endif

namespace InteractiveApp;

public static class Bootstrapper
{
    public static class Globals
    {
        // Global events
        public static event EventHandler WillResignActive = delegate {};
        public static void InvokeWillResignActive() 
            => WillResignActive?.Invoke(null, EventArgs.Empty);

        public static event EventHandler FrameworkInitializationCompleted = delegate {};
        public static void InvokeFrameworkInitializationCompleted() 
        {
            RegisterAfterFrameworkInitializationCompleted();
            FrameworkInitializationCompleted?.Invoke(null, EventArgs.Empty);
        }

#if IOS
        // Alternative touch events for IOS
        public static event Action<NSSet, UIEvent?> TouchesBegan = delegate {};
        public static void InvokeTouchesBegan(NSSet touches, UIEvent? evt) 
            => TouchesBegan.Invoke(touches, evt);

        public static event Action<NSSet, UIEvent?> TouchesMoved = delegate {};
        public static void InvokeTouchesMoved(NSSet touches, UIEvent? evt) 
            => TouchesMoved.Invoke(touches, evt);

        public static event Action<NSSet, UIEvent?> TouchesEnded = delegate {};
        public static void InvokeTouchesEnded(NSSet touches, UIEvent? evt) 
            => TouchesEnded.Invoke(touches, evt);

        public static event Action<NSSet, UIEvent?> TouchesCancelled = delegate {};
        public static void InvokeTouchesCancelled(NSSet touches, UIEvent? evt) 
            => TouchesCancelled?.Invoke(touches, evt);
#endif
    }

    public static TopLevel? GetTopLevel()
    {
        TopLevel? topLevel = null;
        var lifetime = Application.Current?.ApplicationLifetime;
        
        if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            topLevel = desktop.MainWindow;
        }
        else if (lifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            topLevel = singleViewPlatform.MainView as TopLevel;
        }

        return topLevel;
    }

    public static IReadonlyDependencyResolver Resolver
    {
        get { return Locator.Current; }
    }
    
    public static void Register()
    {
        var services = Locator.CurrentMutable;
        services.RegisterConstant(new DataModel());
        var startingView = new StartingScreenViewModel();
        services.RegisterConstant(startingView);
        services.RegisterConstant(new MainViewModel(startingView));
    }

    private static void RegisterAfterFrameworkInitializationCompleted()
    {
        var services = Locator.CurrentMutable;
        var topLevel = GetTopLevel();
        if (topLevel != null)
        {
            services.RegisterConstant<IFilesUtility>(new FilesUtility(topLevel));
        }
    }
}