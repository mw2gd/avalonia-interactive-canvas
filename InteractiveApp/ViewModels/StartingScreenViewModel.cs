using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using InteractiveApp.Models;
using InteractiveApp.Utility;
using ReactiveUI;
using Splat;

namespace InteractiveApp.ViewModels;

public partial class StartingScreenViewModel : ViewModelBase
{   
    public ICommand StartingViewClickCommand {get;}

    public StartingScreenViewModel()
    {
        StartingViewClickCommand = new RelayCommand(HandleClick);
    }

    private static async void HandleClick()
    {
        IFilesUtility? filesUtility = Bootstrapper.Resolver.GetService<IFilesUtility>();
        MainViewModel? mainViewModel = Bootstrapper.Resolver.GetService<MainViewModel>();
        if (mainViewModel != null && filesUtility != null)
        {
            var bitmaps = await filesUtility.OpenFileAsBitmaps();
            mainViewModel.CurrentView = new DrawingViewModel(bitmaps);
        }
    }
}
