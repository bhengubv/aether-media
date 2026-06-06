using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using AetherMesh.Media.Desktop.ViewModels;

namespace AetherMesh.Media.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Overload used by the DI-aware application host.
    /// DataContext is resolved from the service provider rather than being
    /// constructed inline in App.axaml.cs.
    /// </summary>
    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
