using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SI_DevCenter.Helpers;
using SI_DevCenter.Models;
using SI_DevCenter.Repositories;
using System.Text;
using System.Windows;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    private const int MAX_LOG_COUNT = 1000;
    public IList<TabListData> TabListDatas { get; set; }

    private TabListData? _selectedTabListData;
    public TabListData? SelectedTabListData
    {
        get => _selectedTabListData;
        set
        {
            if (value != _selectedTabListData)
            {
                _selectedTabListData = value;
                if (_selectedTabListData != null && _selectedTabListData.Id != 0)
                {
                    _selectedTabListData.Id = 1;
                }
                OnPropertyChanged(nameof(SelectedTabListData));
            }
        }
    }

    // 싱글라인 추가
    private void OutputLog(LogKind kind, string message, bool AutoClear = true)
    {
        var listData = TabListDatas[(int)kind];
        string dt_msg = DateTime.Now.ToString("HH:mm:ss.fff : ") + message;
        if (AutoClear && listData.Items.Count > MAX_LOG_COUNT) listData.Items.Clear();
        listData.Items.Add(dt_msg);
        if (listData != SelectedTabListData)
            listData.Id = 4;
        else listData.Id = 1;
    }

    // 멀티라인 추가
    private void OutputLog(LogKind kind, IList<string> messages)
    {
        var listData = TabListDatas[(int)kind];
        foreach (var item in messages)
        {
            listData.Items.Add(item);
        }
        if (listData != SelectedTabListData)
            listData.Id = 4;
        else listData.Id = 1;
    }

    // 로그지우기
    private void OutputLogClear(LogKind kind)
    {
        var listData = TabListDatas[(int)kind];
        listData.Items.Clear();
    }

    // 리스트 더블클릭 : 최근조회TR
    private void ListBox_MouseDoubleClick(string Text)
    {
        if (SelectedTabListData != null && SelectedTabListData.Name.Equals("최근조회TR"))
        {
            var vals = Text.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length > 2)
            {
                string code = vals[vals.Length - 2].Trim();
                TRData? trData = HDFTrManager.GetTRData(code);
                if (trData != null)
                {
                    _propertyTarget = null;
                    SetResultWithFilePath(trData.FilePath);
                }
            }
        }
    }

    private string? _DoubleClickedItem;
    public string? DoubleClickedItem
    {
        get => _DoubleClickedItem;
        set
        {
            if (!string.Equals(_DoubleClickedItem, value))
            {
                _DoubleClickedItem = value;
                if (_DoubleClickedItem != null) ListBox_MouseDoubleClick(_DoubleClickedItem);
            }
        }
    }

    [RelayCommand]
    void Logs_Menu_Copy()
    {
        if (SelectedTabListData is not null)
        {
            var lines = SelectedTabListData.Items;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string line in lines)
            {
                stringBuilder.AppendLine(line);
            }
            string sAll = stringBuilder.ToString();
            if (sAll.Length > 0)
            {
                Clipboard.SetText(sAll);
            }
        }
    }

    [RelayCommand]
    void Logs_Menu_Clear()
    {
        if (SelectedTabListData is not null)
        {
            SelectedTabListData.Items.Clear();
            SelectedTabListData.Id = 0;
        }
    }

    [RelayCommand]
    void Logs_Menu_AllClear()
    {
        foreach (var data in TabListDatas)
        {
            data.Items.Clear();
            data.Id = 0;
        }
    }

    [ObservableProperty]
    string? _SelectedLogListItem;

    [RelayCommand]
    void Logs_Menu_RemoveBroad()
    {
        if (_loginState == LoginState.LOGINED)
        {
            if (SelectedTabListData != null && SelectedTabListData.Name.Equals("OnGetBroadData"))
            {
                if (SelectedLogListItem != null)
                {
                    int nRealTypeTextIndex = SelectedLogListItem.IndexOf("nRealType=");
                    if (nRealTypeTextIndex != -1)
                    {
                        int.TryParse(SelectedLogListItem.Substring(nRealTypeTextIndex + "nRealType=".Length, 4), out int nRealType);
                        if (nRealType != 0)
                            _axHDFCommAgent.CommRemoveBroad(string.Empty, nRealType);
                    }
                }
            }
        }
    }
}

