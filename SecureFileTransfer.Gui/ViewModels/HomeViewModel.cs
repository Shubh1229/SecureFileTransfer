using CommunityToolkit.Mvvm.Input;
using System;

namespace SecureFileTransfer.Gui.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly Action<ViewModelBase> _navigate;

    public HomeViewModel(Action<ViewModelBase> navigate)
    {
        _navigate = navigate;
    }

    [RelayCommand]
    private void NavigateToHost() => _navigate(new HostViewModel(() => _navigate(this)));

    [RelayCommand]
    private void NavigateToClient() => _navigate(new ClientViewModel(() => _navigate(this)));

    [RelayCommand]
    private void NavigateToSettings() => _navigate(new SettingsViewModel(() => _navigate(this)));
}
