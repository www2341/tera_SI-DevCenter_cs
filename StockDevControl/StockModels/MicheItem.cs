using CommunityToolkit.Mvvm.ComponentModel;

namespace StockDevControl.StockModels
{
    public partial class MicheItem : ObservableObject
    {
        public MicheItem(AccountInfo 계좌정보, StockItemInfo 종목정보, string 주문번호, string 원주문번호, OrderType 매매구분, double 주문가격, int 주문수량, int 주문잔량, double STOP가격)
        {
            this.계좌정보 = 계좌정보;
            this.종목정보 = 종목정보;
            _주문번호 = 주문번호;
            _원주문번호 = 원주문번호;
            this.매매구분 = 매매구분;
            this.주문가격 = 주문가격;
            this.주문수량 = 주문수량;
            this.주문잔량 = 주문잔량;
            this.STOP가격 = STOP가격;
        }

        public AccountInfo 계좌정보 { get; set; }
        public StockItemInfo 종목정보 { get; set; }
        [ObservableProperty]
        string _주문번호;
        [ObservableProperty]
        string _원주문번호;
        public OrderType 매매구분 { get; set; }
        [ObservableProperty]
        double _주문가격;
        [ObservableProperty]
        int _주문수량;
        [ObservableProperty]
        int _주문잔량;
        public int 체결수량 => 주문수량 - 주문잔량;
        [ObservableProperty]
        double _STOP가격;
        // 전략구분
        // 주문일자
        // 주문시간
        // 통화
    }
}
