using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.iOS;
using Avalonia.ReactiveUI;
using CoreGraphics;
using DynamicData;
using Foundation;
using UIKit;

namespace InteractiveApp.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Register for notifications        
        NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillResignActiveNotification,
            (NSNotification notification) => Bootstrapper.Globals.InvokeWillResignActive()
        );

        Bootstrapper.Register();

        Bootstrapper.Globals.FrameworkInitializationCompleted += OnFrameworkInitializationCompleted;

        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseReactiveUI();
    }

    // This will be triggered by Avalonia.Application
    private void OnFrameworkInitializationCompleted(object? sender, EventArgs e)
    {
        if (Window?.RootViewController?.View == null) return;
        
        var rootViewControllerView = Window.RootViewController.View;
        TouchTrackingView view = new TouchTrackingView(rootViewControllerView);
        rootViewControllerView.AddSubview(view); 
    }
}

