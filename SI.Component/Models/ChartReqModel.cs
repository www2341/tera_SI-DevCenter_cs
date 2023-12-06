using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockDevControl.StockModels;
using System.Windows;

namespace SI.Component.Models
{
    public partial class ChartReqModel : ObservableObject
    {
        public ChartReqModel(string TagName, AccountInfo.KIND groupKind)
        {
            Title = TagName;
            TitleBarVisibility = Title.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            GroupKind = groupKind;

            _selectedChartRound = ChartRound.일;
            _selectedChartInterval = string.Empty;

            _selectedChartInterval = "1";
            _selectedDataCount = "100";

            _resultText = string.Empty;
            _codeText = string.Empty;
        }
        public Action<ChartReqModel, string>? ChartReqCommand;

        public Func<ChartReqModel, string>? MakeChartReqCode;
        public AccountInfo.KIND GroupKind { get; }
        public string Title { get; }
        public Visibility TitleBarVisibility { get; }

        public IList<StockItemInfo>? 종목리스트 { get; set; }
        [ObservableProperty] StockItemInfo? _selected종목;
        [ObservableProperty] ChartRound _selectedChartRound;
        [ObservableProperty] string _selectedChartInterval;
        [ObservableProperty] string _selectedDataCount;

        [RelayCommand]
        void Action(string action_name) => ChartReqCommand?.Invoke(this, action_name);

        public bool NextEnabled { get; set; }

        [ObservableProperty] int _nRqId;
        [ObservableProperty] int _ReceivedDataCount;
        [ObservableProperty] DateTime _receivedTime;
        [ObservableProperty] string _resultText;
        [ObservableProperty] string _codeText;


        partial void OnSelectedChartRoundChanged(ChartRound value) => UpdateCodeText();

        partial void OnSelectedChartIntervalChanged(string value) => UpdateCodeText();
        partial void OnSelectedDataCountChanged(string value) => UpdateCodeText();
        partial void OnSelected종목Changed(StockItemInfo? value) => UpdateCodeText();

        public bool EnableUpdateCodeText;
        private void UpdateCodeText()
        {
            if (EnableUpdateCodeText == false) return;
            if (MakeChartReqCode != null)
                CodeText = MakeChartReqCode(this);
        }
    }
}
