using CommunityToolkit.Mvvm.ComponentModel;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewModelBase currentPage = null!;

    public MainWindowViewModel()
    {
        new Initialize();
        var homeVm = new HomeViewModel(page => CurrentPage = page);
        CurrentPage = homeVm;
    }
}