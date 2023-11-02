using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SI_DevCenter.Helpers;

namespace SI_DevCenter.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        private readonly IAppRegistry _appRegistry;

        public MainViewModel(IAppRegistry appRegistry)
        {
            _appRegistry = appRegistry;
            _Title = "SI-DevCenter";
            _StatusText = "Ready";
        }

        [ObservableProperty]
        private string _Title;

        [ObservableProperty]
        private string _StatusText;

        [RelayCommand]
        private void Loaded()
        {
        }

        [RelayCommand]
        private void Closed()
        {
        }

        [RelayCommand(CanExecute = nameof(CanMenuLogin))]
        private void MenuLogin()
        {
        }

        private bool CanMenuLogin()
        {
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanMenuLogout))]
        private void MenuLogout()
        {
        }

        private bool CanMenuLogout()
        {
            return true;
        }

        [RelayCommand]
        private void MenuExit()
        {
            System.Windows.Application.Current.Shutdown();
        }

    }
}

