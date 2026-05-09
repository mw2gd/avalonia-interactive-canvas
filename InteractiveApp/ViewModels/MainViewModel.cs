using Avalonia.Controls;
using InteractiveApp.Models;
using ReactiveUI;
using Splat;

namespace InteractiveApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{   
    private object _currentView;
    public object CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }
    public MainViewModel()
    {
        _currentView = new StartingScreenViewModel();
    }

    public MainViewModel(object view)
    {
        _currentView = view;
    }
}
