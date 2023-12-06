using StockDevControl.StockModels;

namespace SI_DevCenter.Models.SI
{
    public class HWFutureOrderModel : FutureOrderModel
    {
        public enum OrderSpecMain
        {
            지정가 = 1,
            시장가 = 2,
            STOP = 3,
            STOP지정가 = 4,
        }
        public enum OrderSpecSub
        {
            DAY = 0,
            GTC = 1,
            IOC = 3,
            FOK = 4,
            GTD = 6,
        }
        public HWFutureOrderModel() : base(OrderSpecMain.지정가, OrderSpecSub.DAY)
        {
            IsStop가격Visibled = true;
        }
        protected override void OnChanged구분()
        {
            var enum지정구분 = (OrderSpecMain)지정구분;
            var enum체결구분 = (OrderSpecSub)체결구분;

            주문가격Enabled = enum지정구분 == OrderSpecMain.지정가 || enum지정구분 == OrderSpecMain.STOP지정가;

            IsIOC수량Visibled = enum체결구분 == OrderSpecSub.IOC;
            IsGTDDateVisibled = enum체결구분 == OrderSpecSub.GTD;
            Stop가격Enabled = enum지정구분 == OrderSpecMain.STOP || enum지정구분 == OrderSpecMain.STOP지정가;
        }
    }
}
