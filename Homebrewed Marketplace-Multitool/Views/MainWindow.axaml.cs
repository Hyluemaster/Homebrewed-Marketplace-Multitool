using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Homebrewed_Marketplace_Multitool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnLibraryClick(object? sender, RoutedEventArgs e)
    {
        new LibraryWindow().Show(this);
    }

    private void OnUserClick(object? sender, RoutedEventArgs e)
    {
        new UserWindow().Show(this);
    }
}