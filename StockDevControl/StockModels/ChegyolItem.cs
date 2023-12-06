namespace StockDevControl.StockModels
{
    public class ChegyolItem
    {
        public ChegyolItem(AccountInfo 계좌정보, StockItemInfo 종목정보, string 주문번호)
        {
            this.계좌정보 = 계좌정보;
            this.종목정보 = 종목정보;
            this.주문번호 = 주문번호;
        }

        public AccountInfo 계좌정보 { get; set; }
        public StockItemInfo 종목정보 { get; set; }
        public string 주문번호 { get; set; }
        public OrderType 매매구분 { get; set; }
        public double 주문가격 { get; set; }
        public int 체결수량 { get; set; }
        public DateTime 체결시간 { get; set; }
        public DateTime 거래소시간 { get; set; }
        // 통화
    }
}
