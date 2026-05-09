using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InteractiveApp.Controls;
using InteractiveApp.ViewModels;

namespace InteractiveApp.Views;

public partial class DrawingView : UserControl
{
    public DrawingView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var drawingViewModel = DataContext as DrawingViewModel;
        
        if (drawingViewModel != null && drawingViewModel.Bitmaps != null)
        {
            StackPanel stackPanel = new StackPanel()
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
                Spacing = 5,
            };
            foreach (var bitmap in drawingViewModel.Bitmaps)
            {
                stackPanel.Children.Add(new DrawingCanvas(bitmap));
            }
            _interactiveCanvas.Child = stackPanel;
        }
        else
        {
            _interactiveCanvas.Child = new DrawingCanvas();
        }
    }
}