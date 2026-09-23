using CommunityToolkit.Mvvm.ComponentModel;

namespace Homebrewed_Marketplace_Multitool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}
