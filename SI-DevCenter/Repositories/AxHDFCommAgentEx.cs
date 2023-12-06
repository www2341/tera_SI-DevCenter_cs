using HDFCommAgent.NET;
using SI_DevCenter.Models;
using StockDevControl.StockModels;

namespace SI_DevCenter.Repositories
{
    internal class AxHDFCommAgentEx : AxHDFCommAgent
    {
        public AxHDFCommAgentEx(IntPtr hWndParent) : base(hWndParent)
        {
            LoginInfo = new LoginInfo();
            AccountInfos = new List<AccountInfo>();
            HWFutureItemInfos = new List<StockItemInfo>();
            HWFutureKwanItemInfos = new List<StockItemInfo>();
            KRFutureItemInfos = new List<StockItemInfo>();
            _CodeToItemDictionary = new Dictionary<string, StockItemInfo>(StringComparer.Ordinal);
        }

        // 로그인 정보
        public LoginInfo LoginInfo { get; }

        // 계좌정보
        public IList<AccountInfo> AccountInfos { get; }

        // 아이템 정보
        public IList<StockItemInfo> HWFutureItemInfos { get; }
        public IList<StockItemInfo> HWFutureKwanItemInfos { get; }
        public IList<StockItemInfo> KRFutureItemInfos { get; }
        private readonly IDictionary<string, StockItemInfo> _CodeToItemDictionary;
        public StockItemInfo? GetItemInfo(string Code)
        {
            StockItemInfo? result;
            _CodeToItemDictionary.TryGetValue(Code, out result);
            return result;
        }

        public void ReMakeCodeToItemnfoDictionary()
        {
            _CodeToItemDictionary.Clear();

            foreach (var item in HWFutureItemInfos)
                _CodeToItemDictionary.Add(item.종목코드, item);

            foreach (var item in KRFutureItemInfos)
                _CodeToItemDictionary.Add(item.종목코드, item);
        }
    }
}
