using CommunityToolkit.Mvvm.Input;
using SI.Component.Models;
using StockDevControl.StockModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SI.Component.Components
{
    /// <summary>
    /// Interaction logic for HWFutureOrderView.xaml
    /// </summary>
    public partial class FutureOrderComponent : UserControl, IUserTool, INotifyPropertyChanged
    {
        private FutureOrderModel _orderModel;
        private Func<FutureOrderComponent, string, string> _reqCommand;
        public FutureOrderComponent(string title, FutureOrderModel orderModel, Func<FutureOrderComponent, string, string> reqCommand)
        {
            InitializeComponent();

            Title = title;
            TitleBarVisibility = (title.Length > 0) ? Visibility.Visible : Visibility.Collapsed;
            _orderModel = orderModel;
            _reqCommand = reqCommand;
            _CodeText = string.Empty;

            조회Command = new RelayCommand<string?>(조회);
            _orderModel.OrderCommand = 조회Command;

            실시간요청Command = new RelayCommand(실시간요청);

            _orderModel.PropertyChanged += _orderModel_PropertyChanged;
            CodeText = _reqCommand(this, "변환요청");

            DataContext = this;
        }

        private void _orderModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            CodeText = _reqCommand(this, "변환요청");
        }

        public void CloseTool()
        {
            DataContext = null;
            _orderModel.PropertyChanged -= _orderModel_PropertyChanged;
        }

        public string Title { get; }
        public Visibility TitleBarVisibility { get; }
        public FutureOrderModel OrderModel => _orderModel;

        private string _CodeText;
        public string CodeText
        {
            get => _CodeText;
            set
            {
                if (!_CodeText.Equals(value))
                {
                    _CodeText = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IList<JangoItem>? JangoItems
        {
            get
            {
                if (_orderModel.Selected계좌 == null)
                    return null;
                return _orderModel.Selected계좌.JangoItems;
            }
        }
        public JangoItem? SelectedJangoItem { get; set; }
        public IList<MicheItem>? MicheItems
        {
            get
            {
                if (_orderModel.Selected계좌 == null)
                    return null;
                return _orderModel.Selected계좌.MicheItems;
            }
        }
        public MicheItem? SelectedMicheItem { get; set; }

        public int SelectedTabIndex { get; set; }
        public ICommand 조회Command { get; }

        private void 조회(string? action)
        {
            if (action == null) return;
            if (action.Equals("계좌요청"))
            {
                if (SelectedTabIndex == 0) // 잔고
                {
                    _reqCommand(this, "잔고요청");
                }
                else if (SelectedTabIndex == 1) // 미체결
                {
                    _reqCommand(this, "미체결요청");
                }
            }
            else
            {
                string result = _reqCommand(this, action);
                CodeText += result;
            }
        }

        public ICommand 실시간요청Command { get; }
        private void 실시간요청()
        {
            _reqCommand(this, "실시간현재가요청");
        }

        public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (PropertyChanged is not null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void 잔고Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedJangoItem == null) return;
            OrderModel.매매구분 = SelectedJangoItem.매매구분 == OrderType.매수 ? OrderType.매도 : OrderType.매수;
            OrderModel.주문수량 = SelectedJangoItem.보유수량;
        }

        private void 미체결Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedMicheItem == null) return;
            OrderModel.매매구분 = OrderType.정정취소;
            OrderModel.주문번호 = SelectedMicheItem.주문번호;
            OrderModel.주문가격 = SelectedMicheItem.주문가격;
            OrderModel.주문수량 = SelectedMicheItem.주문수량;
        }
    }
}
