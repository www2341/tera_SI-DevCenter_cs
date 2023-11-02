using CommunityToolkit.Mvvm.ComponentModel;
using SI_DevCenter.Models;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    [ObservableProperty]
    public IList<string>? _TRLists;

    private async Task LoadTRListsAsync()
    {
        if (_loginState < LoginState.CONNECTED) return;

        var task = Task.Factory.StartNew(() =>
        {
            string path = "C:\\EZ\\SIAPI";
            path += "\\TrData";

            // 폴더내의 전테dat파일 불러온다
            string[] files = System.IO.Directory.GetFiles(path, "*.dat");
            if (files.Length == 0) return null;


            return files.ToList();
        });

        var root = await task.ConfigureAwait(true);
        if (root != null)
        {
            TRLists = root;
            LogOut($"Loaded: TR목록({root.Count})");
        }
    }
}

