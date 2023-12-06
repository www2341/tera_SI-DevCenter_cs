using SI_DevCenter.Repositories;
using System.IO;

namespace SI_DevCenter.Models
{
    internal class TRData
    {
        public TRData(string FilePath)
        {
            this.FilePath = FilePath;
            FileTitle = Path.GetFileNameWithoutExtension(FilePath);
            TRCode = FileTitle.Split('_')[0];
        }

        public string FilePath { get; }
        public string FileTitle { get; }
        public string TRCode { get; }
        public string TRName { get; set; } = string.Empty;
        public int OutputCnt;
        public int DataHeader;
        public IList<(string, string)> InputDatas { get; } = new List<(string, string)>();

        public IList<string> OutputNames { get; } = new List<string>();
        public IList<int> OutputSizes { get; } = new List<int>();
        public int OutputTotalSize;

        public int OutRec1RowCountDigit;
        public IList<string> OutRec1Names { get; } = new List<string>();
        public IList<int> OutRec1Sizes { get; } = new List<int>();
        public int OutRec1TotalSize;

        public int OutRec2RowCountDigit;
        public IList<string> OutRec2Names { get; } = new List<string>();
        public IList<int> OutRec2Sizes { get; } = new List<int>();
        public int OutRec2TotalSize;

        public HDFTrManager.REQKindClasses? DefReqData;
    }
}
