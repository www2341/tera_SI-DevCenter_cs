using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HDFCommAgent.NET;
using SI_DevCenter.Helpers;
using SI_DevCenter.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Interop;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    private readonly IAppRegistry _appRegistry;
    private readonly AxHDFCommAgent _axHDFCommAgent;
    private LoginState _loginState;

    public IList<string> Logs { get; } = new ObservableCollection<string>();

    public MainViewModel(IAppRegistry appRegistry)
    {
        _appRegistry = appRegistry;
        _Title = "SI-DevCenter" + (Environment.Is64BitProcess ? "-64비트" : "-32비트");
        _StatusText = "Ready";

        IntPtr Handle = new WindowInteropHelper(Application.Current.MainWindow).EnsureHandle();
        _axHDFCommAgent = new AxHDFCommAgent(Handle);
        if (_axHDFCommAgent.Created)
            _loginState = LoginState.CREATED;

        LogOut("Application Start");
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

    [RelayCommand]
    private void MenuExit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void LogOut(string message)
    {
        string text = DateTime.Now.ToString("u") + " - " + message;
        Logs.Add(text);
    }
}

