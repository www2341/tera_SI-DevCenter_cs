using System.Collections.ObjectModel;

namespace StockDevControl.StockModels
{
    public class AccountInfo
    {
        public string 계좌번호 { get; }
        public string 계좌명 { get; }

        public enum KIND
        {
            UNKNOWN = 0,
            국내 = 9,
            해외 = 1,
            FX = 2,
        }
        public KIND 계좌구분 { get; }

        public AccountInfo(string 계좌번호, string 계좌명, KIND 계좌구분)
        {
            this.계좌번호 = 계좌번호;
            this.계좌명 = 계좌명;
            this.계좌구분 = 계좌구분;
            JangoItems = new ObservableCollection<JangoItem>();
            MicheItems = new ObservableCollection<MicheItem>();
        }

        public override string ToString() => $"[{계좌번호}]  {계좌명}";

        public bool JangoInited;
        public IList<JangoItem> JangoItems { get; }

        public bool MicheInited;
        public IList<MicheItem> MicheItems { get; }
    }
}

