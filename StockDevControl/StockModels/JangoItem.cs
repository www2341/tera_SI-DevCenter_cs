using CommunityToolkit.Mvvm.ComponentModel;

namespace StockDevControl.StockModels
{
    public partial class JangoItem : ObservableObject
    {
        public JangoItem(AccountInfo 계좌정보, StockItemInfo 종목정보, OrderType 매매구분, int 보유수량, double 평균단가)
        {
            this.계좌정보 = 계좌정보;
            this.종목정보 = 종목정보;
            this.매매구분 = 매매구분;
            this.보유수량 = 보유수량;
            this.평균단가 = 평균단가;
        }

        public AccountInfo 계좌정보 { get; }
        public StockItemInfo 종목정보 { get; }
        [ObservableProperty]
        OrderType _매매구분;
        [ObservableProperty]
        int _보유수량;
        [ObservableProperty]
        double _평균단가;
        // 현재가
        [ObservableProperty]
        double _손익;
        // 통화
    }
}
