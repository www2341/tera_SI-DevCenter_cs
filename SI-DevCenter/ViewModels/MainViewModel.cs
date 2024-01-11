using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SI_DevCenter.Helpers;
using SI_DevCenter.Models;
using SI_DevCenter.Repositories;
using SI_DevCenter.Views;
using StockDevControl.Models;
using StockDevControl.StockModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    private readonly string _baseTitle;
    private readonly string _appVersion;
    private IList<GithubTagInfo>? _releaseTags;

    private static readonly Encoding _krEncoder = Encoding.GetEncoding("EUC-KR");
    private readonly Window _mainWindow;
    private readonly IAppRegistry _appRegistry;
    private readonly AxHDFCommAgentEx _axHDFCommAgent;
    private readonly string _apiFolderPath;
    private LoginState _loginState
    {
        get => _axHDFCommAgent.LoginInfo.State;
        set
        {
            _axHDFCommAgent.LoginInfo.State = value;
        }
    }

    public IList<string> Logs { get; } = new ObservableCollection<string>();

    public MainViewModel(IAppRegistry appRegistry)
    {
        _mainWindow = Application.Current.MainWindow;
        _appRegistry = appRegistry;

        // 메인 윈도우 설정값 로딩
        string session = _mainWindow.GetType().Name;
        int Left = _appRegistry.GetValue(session, "Left", 0);
        int Top = _appRegistry.GetValue(session, "Top", 0);
        int Width = _appRegistry.GetValue(session, "Width", 1250);
        int Height = _appRegistry.GetValue(session, "Height", 760);

        TabTreeWidth = new(_appRegistry.GetValue(session, nameof(TabTreeWidth), 410));
        TabListHeight = new(_appRegistry.GetValue(session, nameof(TabListHeight), 150));
        PropertyWidth = new(_appRegistry.GetValue(session, nameof(PropertyWidth), 270));

        if (Left != 0) _mainWindow.Left = Left;
        if (Top != 0) _mainWindow.Top = Top;
        if (Width != 0) _mainWindow.Width = Width;
        if (Height != 0) _mainWindow.Height = Height;

        _mainWindow.Closed += (s, e) =>
        {
            _appRegistry.SetValue(session, "Left", (int)_mainWindow!.Left);
            _appRegistry.SetValue(session, "Top", (int)_mainWindow.Top);
            _appRegistry.SetValue(session, "Width", (int)_mainWindow.Width);
            _appRegistry.SetValue(session, "Height", (int)_mainWindow.Height);

            _appRegistry.SetValue(session, nameof(TabTreeWidth), (int)TabTreeWidth.Value);
            _appRegistry.SetValue(session, nameof(TabListHeight), (int)TabListHeight.Value);
            _appRegistry.SetValue(session, nameof(PropertyWidth), (int)PropertyWidth.Value);

            SaveToolsData();
        };

        // 바인딩 데이터 설정
        var assemblyName = System.Windows.Application.ResourceAssembly.GetName();
        _appVersion = $"{assemblyName.Version!.Major}.{assemblyName.Version.Minor}";
        _baseTitle = $"{assemblyName.Name} v{_appVersion} - {(Environment.Is64BitProcess ? "64비트" : "32비트")}";

        _title = _baseTitle;
        _statusText = "Ready";
        _ResultPath = string.Empty;
        _ResultText = string.Empty;
        _EquipText = string.Empty;

        // 로그 리스트 설정
        TabListDatas = new List<TabListData>();
        string[] logKinds = Enum.GetNames(typeof(LogKind));
        foreach (string logKind in logKinds)
        {
            TabListDatas.Add(new(logKind));
        }
        SelectedTabListData = TabListDatas[0];

        OutputLog(LogKind.LOGS, "Application Start");

        TabTreeDatas =
        [
            new(8, "전체파일"),
            new(0, "TR목록"),
            new(2, "서비스목록"),
            new(3, "함수목록"),
            new(4, "사용자기능"),
        ];

        // 사용자기능
        IdTextItem user_root = new(4, "사용자기능") { IsExpanded = true };
        user_root.AddChild(new(9, "API정보") { Tag = "API정보" });
        user_root.AddChild(new(9, "계좌정보") { Tag = "계좌정보" });

        IdTextItem tools_주문 = new(7, "주문요청") { IsExpanded = true };
        tools_주문.AddChild(new(9, "국내선물주문") { Tag = "국내선물주문" });
        tools_주문.AddChild(new(9, "해외선물주문") { Tag = "해외선물주문" });
        user_root.AddChild(tools_주문);

        IdTextItem tools_차트 = new(7, "차트요청") { IsExpanded = true };
        tools_차트.AddChild(new(9, "국내선물 차트요청") { Tag = "국내선물 차트요청" });
        tools_차트.AddChild(new(9, "해외선물 차트요청") { Tag = "해외선물 차트요청" });
        user_root.AddChild(tools_차트);

        TabTreeDatas[4].OrgItems = new List<object>() { user_root };

        SelectedTabTreeData = TabTreeDatas[0];

        // OCX 설정
        //_apiFolderPath = OcxPathHelper.GetDLLDirectoryFromProgID("HDFCOMMAGENT.HDFCommAgentCtrl.1");
        // 이미 설치되어 있는 OCX경로

        _apiFolderPath = string.Empty;
        bool bOcxInstalled = false;
        string ocxPath = OcxPathHelper.GetOcxPathFromCLSID("{2A7B5BEF-49EE-4219-9833-DB04D07876CF}");

        string myPath = Assembly.GetEntryAssembly()!.Location;
        string myFolder = Path.GetDirectoryName(myPath)!;

        if (string.IsNullOrEmpty(ocxPath))
        {
            OutputLog(LogKind.LOGS, "Api가 설치되어 있지 않습니다.");
        }
        else
        {
            string ocxPath_32 = string.Empty;
            string ocx_folder = Path.GetDirectoryName(ocxPath)!;
            bOcxInstalled = true;

            // 64비트인 경우 32비트 경로 추가 가져오기
            if (Environment.Is64BitProcess)
            {
                string folder_64 = ocx_folder;
                string ocxPath_64 = ocxPath;
                ocxPath_32 = OcxPathHelper.GetOcxPathFromWOW6432NodeCLSID("{2A7B5BEF-49EE-4219-9833-DB04D07876CF}");
                if (!string.IsNullOrEmpty(ocxPath_32))
                {
                    string folder_32 = Path.GetDirectoryName(ocxPath_32)!;
                    // 64비트와 32비트 경로가 다르다면 
                    if (!folder_64.Equals(folder_32, StringComparison.OrdinalIgnoreCase))
                    {
                        // 64비트 경로에 32비트 ocx 존재한다면?
                        string alias_ocx32_path = folder_64 + @"\HDFCommAgent.ocx";
                        if (File.Exists(alias_ocx32_path))
                        {
                            // 새롭게 OCX등록
                            ReqisterOCX(alias_ocx32_path);
                            ocxPath_32 = alias_ocx32_path;
                        }
                    }
                }
                else
                {
                    // 64비트 경로에 32비트 ocx 존재한다면?
                    string alias_ocx32_path = folder_64 + @"\HDFCommAgent.ocx";
                    if (File.Exists(alias_ocx32_path))
                    {
                        // 새롭게 OCX등록
                        ReqisterOCX(alias_ocx32_path);
                        ocxPath_32 = alias_ocx32_path;
                    }
                    else
                    {
                        bOcxInstalled = false;
                        OutputLog(LogKind.LOGS, "32bit Api가 설치되어 있지 않습니다.");
                    }
                }
            }
            else
            {
                ocxPath_32 = ocxPath;
                // 32비트인 경우 실행 파일 경로에 있는 OCX 이용하기
                if (!myFolder.Equals(ocx_folder, StringComparison.OrdinalIgnoreCase))
                {
                    string alias_ocx32_path = myFolder + @"\HDFCommAgent.ocx";
                    if (File.Exists(alias_ocx32_path))
                    {
                        ReqisterOCX(alias_ocx32_path);
                        ocxPath_32 = alias_ocx32_path;
                    }
                }
            }

            _apiFolderPath = Path.GetDirectoryName(ocxPath_32)!;

            Environment.CurrentDirectory = _apiFolderPath;
            HDFTrManager.ApiFolderPath = _apiFolderPath;

            _title += $" ({_apiFolderPath})";
        }

        IntPtr Handle = new WindowInteropHelper(_mainWindow).EnsureHandle();
        _axHDFCommAgent = new AxHDFCommAgentEx(Handle);
        if (_axHDFCommAgent.Created)
        {
            _loginState = LoginState.CREATED;
            //_axHDFCommAgent.OnRealData += _axHDFCommAgent_OnRealData;
            _axHDFCommAgent.OnDataRecv += _axHDFCommAgent_OnDataRecv;
            //_axHDFCommAgent.OnGetData += _axHDFCommAgent_OnGetData;
            _axHDFCommAgent.OnGetBroadData += _axHDFCommAgent_OnGetBroadData;
            //_axHDFCommAgent.OnGetMsg += _axHDFCommAgent_OnGetMsg;
            _axHDFCommAgent.OnGetMsgWithRqId += _axHDFCommAgent_OnGetMsgWithRqId;
        }
        if (bOcxInstalled)
            LoadTRListsAsync();

        // Component 초기화
        string toolKey = "국내선물 차트요청";
        _krFutureChartModel = new(toolKey, AccountInfo.KIND.국내)
        {
            MakeChartReqCode = MakeChartReqCode,
            ChartReqCommand = ChartReqCommand,
            SelectedChartInterval = _appRegistry.GetValue(toolKey, "ChartInterval", "1"),
            SelectedDataCount = _appRegistry.GetValue(toolKey, "DataCount", "100"),
            SelectedChartRound = GetChartRoundFromString(_appRegistry.GetValue(toolKey, "ChartRound", "일")),
        };

        toolKey = "해외선물 차트요청";
        _hwFutureChartModel = new(toolKey, AccountInfo.KIND.해외)
        {
            MakeChartReqCode = MakeChartReqCode,
            ChartReqCommand = ChartReqCommand,
            SelectedChartInterval = _appRegistry.GetValue(toolKey, "ChartInterval", "1"),
            SelectedDataCount = _appRegistry.GetValue(toolKey, "DataCount", "100"),
            SelectedChartRound = GetChartRoundFromString(_appRegistry.GetValue(toolKey, "ChartRound", "일")),
        };

        _ = CheckVersionAsync();
    }
    async void ReqisterOCX(string path)
    {
        var task = Task.Factory.StartNew(() =>
        {
            return OcxPathHelper.RegisterOCX(path, true);
        });

        int result = await task.ConfigureAwait(true);
        if (result < 0)
        {
            OutputLog(LogKind.LOGS, $"Api를 설치할 수 없습니다: {path}");
        }
    }


    [ObservableProperty]
    private GridLength _tabTreeWidth;

    [ObservableProperty]
    private GridLength _tabListHeight;

    [ObservableProperty]
    private GridLength _propertyWidth;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private string _statusUrl = string.Empty;

    [RelayCommand]
    private void MenuExit()
    {
        Application.Current.Shutdown();
    }

    private async Task CheckVersionAsync()
    {
        // 깃헙에서 최신 버전 정보 가져오기

        _releaseTags = await GithubVersion.GetRepoTagInfos("teranum", "SI-DevCenter").ConfigureAwait(true);
        if (_releaseTags != null && _releaseTags.Count > 0)
        {
            var lastTag = _releaseTags[0];
            if (string.Equals(lastTag.tag_name, _appVersion))
            {
                StatusText = "최신 버전입니다.";
            }
            else
            {
                StatusUrl = lastTag.html_url;
                StatusText = $"새로운 버전({lastTag.tag_name})이 있습니다.";
            }
        }
    }

    [RelayCommand]
    void Menu_Version()
    {
        // 버젼 정보
        if (_releaseTags != null && _releaseTags.Count != 0)
        {
            var versionView = new VersionView(_releaseTags);
            versionView.ShowDialog();
        }
    }

    void SetStatusText(string text)
    {
        StatusText = text;
        StatusUrl = string.Empty;
    }
}

