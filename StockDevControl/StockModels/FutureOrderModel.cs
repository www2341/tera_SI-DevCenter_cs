using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace StockDevControl.StockModels
{
    public abstract partial class FutureOrderModel : ObservableObject
    {
        protected FutureOrderModel(object 지정구분, object 체결구분)
        {
            _지정구분 = 지정구분;
            _체결구분 = 체결구분;

            _비밀번호 = string.Empty;
            _주문번호 = string.Empty;
            _매매구분 = OrderType.매수;
            주문수량 = 1;
            IOC수량 = 1;

            _주문가격Enabled = true;
            GTDDate = DateTime.UtcNow;

        }
        protected abstract void OnChanged구분();

        public IList<AccountInfo>? 계좌리스트 { get; set; }
        //public AccountInfo? Selected계좌 { get; set; }
        [ObservableProperty]
        AccountInfo? _Selected계좌;

        //public string 비밀번호 { get; set; }
        [ObservableProperty]
        string _비밀번호;

        public IList<StockItemInfo>? 종목리스트 { get; set; }
        //public StockItemInfo? Selected종목 { get; set; }
        [ObservableProperty] StockItemInfo? _Selected종목;

        //public OrderType 매매구분 { get; set; }
        [ObservableProperty] OrderType _매매구분;

        object _지정구분;
        public object 지정구분
        {
            get => _지정구분;
            set
            {
                if (_지정구분 != value)
                {
                    _지정구분 = value;
                    OnChanged구분();
                }
            }
        }

        object _체결구분;
        public object 체결구분
        {
            get => _체결구분;
            set
            {
                if (_체결구분 != value)
                {
                    _체결구분 = value;
                    OnChanged구분();
                }
            }
        }

        [ObservableProperty]
        bool _IsIOC수량Visibled;

        [ObservableProperty]
        bool _IsGTDDateVisibled;

        [ObservableProperty]
        int _IOC수량;

        [ObservableProperty]
        DateTime _GTDDate;


        [ObservableProperty]
        string _주문번호;

        [ObservableProperty]
        bool _주문가격Enabled;

        [ObservableProperty]
        double _주문가격;

        [ObservableProperty]
        double _STOP가격;

        public bool IsStop가격Visibled { get; set; }
        [ObservableProperty]
        bool _Stop가격Enabled;

        [ObservableProperty]
        int _주문수량;

        [RelayCommand]
        void UpButton(string target)
        {
            if (target.Equals("주문가격"))
            {
                if (Selected종목 != null)
                {
                    주문가격 += Selected종목.틱사이즈;
                }
            }
            else if (target.Equals("STOP가격"))
            {
                if (Selected종목 != null)
                {
                    STOP가격 += Selected종목.틱사이즈;
                }
            }
            else if (target.Equals("주문수량"))
            {
                주문수량 += 1;
            }
            else if (target.Equals("IOC수량"))
            {
                IOC수량 += 1;
            }
        }

        [RelayCommand]
        void DownButton(string target)
        {
            if (target.Equals("주문가격"))
            {
                if (Selected종목 != null)
                {
                    주문가격 -= Selected종목.틱사이즈;
                }
            }
            else if (target.Equals("STOP가격"))
            {
                if (Selected종목 != null)
                {
                    STOP가격 -= Selected종목.틱사이즈;
                }
            }
            else if (target.Equals("주문수량"))
            {
                if (주문수량 > 1)
                    주문수량 -= 1;
            }
            else if (target.Equals("IOC수량"))
            {
                if (IOC수량 > 1)
                    IOC수량 -= 1;
            }
        }

        public ICommand? OrderCommand { get; set; }

        public bool 주문확인생략 { get; set; }

        [RelayCommand]
        void Set현재가()
        {
            if (Selected종목 != null)
                주문가격 = Selected종목.현재가;
        }
    }
}
