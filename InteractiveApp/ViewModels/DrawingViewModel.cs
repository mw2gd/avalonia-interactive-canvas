using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using InteractiveApp.Models;
using Splat;

namespace InteractiveApp.ViewModels;

public partial class DrawingViewModel : ViewModelBase
{   
    public DataModel DataModelInstance { get; }
    public ICommand DrawingViewClickCommand {get;}
    public List<Bitmap>? Bitmaps {get; set;}

    public DrawingViewModel()
    {
        DataModelInstance = Bootstrapper.Resolver.GetService<DataModel>()!;
        DrawingViewClickCommand = new RelayCommand(HandleClick);
    }

    public DrawingViewModel(List<Bitmap> bitmaps)
    {
        DataModelInstance = Bootstrapper.Resolver.GetService<DataModel>()!;
        DrawingViewClickCommand = new RelayCommand(HandleClick);
        Bitmaps = bitmaps;
    }

    private static void HandleClick()
    {
        var main = Bootstrapper.Resolver.GetService<MainViewModel>();
        var starting = Bootstrapper.Resolver.GetService<StartingScreenViewModel>();
        if (main != null && starting != null)
        {
            main.CurrentView = starting;
        }
    }
}
