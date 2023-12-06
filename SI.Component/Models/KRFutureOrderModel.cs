using StockDevControl.StockModels;

namespace SI_DevCenter.Models.SI
{
    public class KRFutureOrderModel : FutureOrderModel
    {
        public enum OrderSpecMain
        {
            지정가 = 1,
            시장가 = 2,
            조건부 = 3,
            최유리 = 4,
        }
        public enum OrderSpecSub
        {
            FAS = 1,
            FOK = 2,
            FAK = 3,
        }
        public KRFutureOrderModel() : base(OrderSpecMain.지정가, OrderSpecSub.FAS)
        {
        }
        protected override void OnChanged구분()
        {
            var enum지정구분 = (OrderSpecMain)지정구분;
            var enum체결구분 = (OrderSpecSub)체결구분;

            주문가격Enabled = enum지정구분 == OrderSpecMain.지정가;

        }
    }
}
