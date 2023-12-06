using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SI_DevCenter.Models;
using System.IO;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    [ObservableProperty]
    private string _ResultPath;

    [ObservableProperty]
    private string _ResultText;

    [ObservableProperty]
    private int _ResultUndoStackSizeLimite = 10;

    [ObservableProperty]
    private string _EquipText;

    [ObservableProperty]
    int _EquipHeight;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void ResultSave()
    {
        if (string.IsNullOrEmpty(ResultPath))
        {
            OutputLog(LogKind.LOGS, "보관 파일 없음");
            return;
        }

        var ansi_string = _krEncoder.GetBytes(ResultText);
        try
        {
            File.WriteAllBytes(ResultPath, ansi_string);

            ResultUndoStackSizeLimite = 0;
            ResultUndoStackSizeLimite = 10;
        }
        catch (Exception ex)
        {
            OutputLog(LogKind.LOGS, $"파일보관 오류: {ResultPath} ({ex.Message})");
        }
    }
    bool CanSave() => (_propertyTarget is TRData);

    void SetResultText(string text)
    {
        ResultText = text;
        ResultUndoStackSizeLimite = 0;
        ResultUndoStackSizeLimite = 10;
    }

    void AddResultText(string text)
    {
        ResultText += text;
    }

    void SetEquipText(string text)
    {
        EquipHeight = string.IsNullOrEmpty(text) ? 0 : 80;
        EquipText = text;
    }
}

