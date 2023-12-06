using SI_DevCenter.Models;
using StockDevControl.Models;
using System.IO;
using System.Text;

namespace SI_DevCenter.Repositories
{
    internal class HDFTrManager
    {
        private static readonly Encoding _krEncoder = Encoding.GetEncoding("EUC-KR");
        public static string ApiFolderPath = string.Empty;
        private static readonly Dictionary<string, TRData> _codeToTrData = new(StringComparer.Ordinal);
        private static readonly Dictionary<int, TRData> _realtypeToTrData = [];

        public static TRData? GetTRData(string Code)
        {
            if (_codeToTrData.TryGetValue(Code, out TRData trData))
            {
                return trData;
            }
            return null;
        }

        public static TRData? GetTRData(int realtype)
        {
            if (_realtypeToTrData.TryGetValue(realtype, out TRData trData))
            {
                return trData;
            }
            return null;
        }

        public static void ParsingTRData(ref TRData trData, string ansiText)
        {
            // [TRINFO]
            var lines = ansiText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            TRSECTION trSection = TRSECTION.NONE;
            string key = string.Empty;
            string value = string.Empty;
            foreach (string line in lines)
            {
                if (line.IndexOf("[TRINFO]") == 0)
                    trSection = TRSECTION.TRINFO;
                else if (line.IndexOf("[INPUT]") == 0)
                    trSection = TRSECTION.INPUT;
                else if (line.IndexOf("[OUTPUT]") == 0)
                    trSection = TRSECTION.OUTPUT;
                else if (line.IndexOf("@START_OutRec1") == 0)
                {
                    trSection = TRSECTION.OUTREC1;
                    if (SplitKeyValue(line, ref key, ref value))
                    {
                        var vals = value.Split(',');
                        if (vals.Length > 1 && int.TryParse(vals[1], out int digit))
                            trData.OutRec1RowCountDigit = digit;
                    }
                }
                else if (line.IndexOf("@END_OutRec1") == 0)
                    trSection = TRSECTION.END1;
                else if (line.IndexOf("@START_OutRec2") == 0)
                {
                    trSection = TRSECTION.OUTREC2;
                    if (SplitKeyValue(line, ref key, ref value))
                    {
                        var vals = value.Split(',');
                        if (vals.Length > 1 && int.TryParse(vals[1], out int digit))
                            trData.OutRec2RowCountDigit = digit;
                    }
                }
                else if (line.IndexOf("@END_OutRec2") == 0)
                    trSection = TRSECTION.END2;
                else
                {
                    if (line[0] != ';' && SplitKeyValue(line, ref key, ref value, trSection == TRSECTION.INPUT))
                    {
                        if (trSection == TRSECTION.TRINFO)
                        {
                            if (string.Equals(key, "TRName")) trData.TRName = value;
                            else if (string.Equals(key, "OutputCnt")) trData.OutputCnt = Convert.ToInt32(value);
                            else if (string.Equals(key, "DataHeader")) trData.DataHeader = Convert.ToInt32(value);
                        }
                        else if (trSection == TRSECTION.INPUT)
                        {
                            trData.InputDatas.Add((key, value));
                        }
                        else if (trSection == TRSECTION.OUTPUT)
                        {
                            var vals = value.Split(',');
                            int size = Convert.ToInt32(vals[1]);

                            trData.OutputNames.Add(key);
                            trData.OutputSizes.Add(size);
                            trData.OutputTotalSize += size;
                        }
                        else if (trSection == TRSECTION.OUTREC1)
                        {
                            var vals = value.Split(',');
                            int size = Convert.ToInt32(vals[1]);

                            trData.OutRec1Names.Add(key);
                            trData.OutRec1Sizes.Add(size);
                            trData.OutRec1TotalSize += size;
                        }
                        else if (trSection == TRSECTION.OUTREC2)
                        {
                            var vals = value.Split(',');
                            int size = Convert.ToInt32(vals[1]);

                            trData.OutRec2Names.Add(key);
                            trData.OutRec2Sizes.Add(size);
                            trData.OutRec2TotalSize += size;
                        }
                    }
                }
            }

            var Code = trData.TRCode;
            trData.DefReqData = PreDefineReqs.FirstOrDefault(x => x.Code.Equals(Code));

            if (trData.DefReqData == null)
            {
                // 디폴트 목록에 없는 TR경우

                if (int.TryParse(trData.TRCode, out int realtype))
                {
                    trData.DefReqData = new(trData.TRCode, REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.공통, REQKIND_SUB.시세, false);
                }
                //else if (trData.TRCode.Length > 0 && trData.TRCode[0] == 'g')
                //{
                //    trData.DefReqData = new(trData.TRCode, REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.TR, false);
                //}
            }

            bool SplitKeyValue(string text, ref string key, ref string value, bool bFullValue = false)
            {
                // ex DataHeader=5; 2:해외주문, 3:해외조회, 4:국내주문, 5:국내조회
                // out: (DataHeader, 5)
                var key_value = text.Split(bFullValue ? ['=',] : ['=', ';'], StringSplitOptions.RemoveEmptyEntries);
                if (key_value.Length < 2) return false;
                key = key_value[0].Trim();
                value = key_value[1].Trim();
                return true;
            }
        }
        public static TRData LoadTRData(string filepath, IList<string> Errors)
        {
            TRData trData = new(filepath);
            try
            {
                byte[] fileData = File.ReadAllBytes(filepath);
                string ansiText = _krEncoder.GetString(fileData, 0, fileData.Length);

                ParsingTRData(ref trData, ansiText);
            }
            catch (Exception ex)
            {
                /*
                0195 : 매도평균가      = 083, 20, 0, A ; "004" -> 매수평균가      = 083, 20, 0, A ; "004"
                 */
                Errors.Add($"Error: {filepath} : {ex.Message}");
            }
            return trData;
        }
        public static Task<Tuple<IList<TRData>, IList<string>>?> LoadAllTRListsAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                IList<TRData> TRDatas = new List<TRData>();
                IList<string> Errors = new List<string>();
                var defReqs = PreDefineReqs;

                try
                {
                    // api폴더 설정
                    string path = ApiFolderPath + "\\TrData";

                    // 폴더내의 전체 dat파일 불러온다
                    string[] filepaths = Directory.GetFiles(path, "*.dat");
                    if (filepaths.Length == 0) return null;

                    foreach (var filepath in filepaths)
                    {
                        var trData = LoadTRData(filepath, Errors);

                        TRDatas.Add(trData);
                        if (_codeToTrData.TryGetValue(trData.TRCode, out var existTrData))
                        {
                            Errors.Add($"Exist aleady : {existTrData.TRCode} : {existTrData.FilePath}, {filepath} ");
                        }
                        else
                            _codeToTrData.Add(trData.TRCode, trData);
                        int.TryParse(trData.TRCode, out var realType);
                        if (realType != 0)
                        {
                            if (_realtypeToTrData.TryGetValue(realType, out var existRealTr))
                            {
                                Errors.Add($"Exist RealType aleady : {existRealTr.TRCode} : {existRealTr.FilePath}, {filepath} ");
                            }
                            else
                                _realtypeToTrData.Add(realType, trData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Errors.Add(ex.Message);
                }
                return Tuple.Create(TRDatas, Errors);
            });
        }

        internal class REQKindClasses
        {
            public REQKIND_FUNC ReqKind_Func;
            public REQKIND_MASTER ReqKind_Master;
            public REQKIND_MAIN ReqKind_Main;
            public REQKIND_SUB ReqKind_Sub;
            public string Code;
            public bool NeedAccount; // 계좌관련 TR

            public REQKindClasses(string Code
                , REQKIND_FUNC ReqKind_Func
                , REQKIND_MASTER ReqKind_Master
                , REQKIND_MAIN ReqKind_Main
                , REQKIND_SUB ReqKind_Sub
                , bool NeedAccount = false
                )
            {
                this.Code = Code;
                this.ReqKind_Func = ReqKind_Func;
                this.ReqKind_Master = ReqKind_Master;
                this.ReqKind_Main = ReqKind_Main;
                this.ReqKind_Sub = ReqKind_Sub;
                this.NeedAccount = NeedAccount;
            }
        }

        public static readonly REQKindClasses[] PreDefineReqs =
            [
                // 주문
                // 국내
                new("g12001.DO1601&", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.국내, REQKIND_SUB.None, true),
                new("g12001.DO1901&", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.국내, REQKIND_SUB.None, true),
                new("g12001.DO1701&", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.국내, REQKIND_SUB.None, true),
                new("g12001.DO2201&", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.국내, REQKIND_SUB.None, true),
                new("g12001.DO2101&", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.국내, REQKIND_SUB.None, true),
                new("g12001.DO2001&", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.국내, REQKIND_SUB.None, true),
                // 해외
                new("g12003.AO0401%", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.해외, REQKIND_SUB.None, true),
                new("g12003.AO0402%", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.해외, REQKIND_SUB.None, true),
                new("g12003.AO0403%", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.해외, REQKIND_SUB.None, true),
                // FX
                new("g12003.AO0501%", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.FX, REQKIND_SUB.None, true),
                new("g12003.AO0502%", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.FX, REQKIND_SUB.None, true),
                new("g12003.AO0503%", REQKIND_FUNC.CommJumunSvr, REQKIND_MASTER.주문, REQKIND_MAIN.FX, REQKIND_SUB.None, true),

                // 조회
                // 국내
                new("g11002.DQ0104&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0107&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0110&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ1305&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0116&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0119&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0122&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ1306&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0125&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ1303&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0217&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0242&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0502&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0509&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0521&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR, true),
                new("g11002.DQ0622&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR),
                new("g11002.DQ1211&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR),
                new("g11002.DQ1302&", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR),
                new("v90003", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.TR),
                new("l41600", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("l41601", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("l41602", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("l41603", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("l41619", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("s20001", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("s31001", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("s10001", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),
                new("l41700", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.국내, REQKIND_SUB.FID),

                // 해외
                new("g11004.AQ0128%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR),
                new("g11004.AQ0301%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0302%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0401%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0402%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0403%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0404%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0405%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0408%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0409%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0415%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0450%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                //new("g11004.AQ0495%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR), // 공통에 들어가 있음
                new("g11004.AQ0602%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0605%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0607%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0636%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR),
                new("g11004.AQ0712%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0715%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR),
                new("g11004.AQ0725%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR),
                new("g11004.AQ0805%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0824%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0807%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0451%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("o44005", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR),
                new("o51000", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                new("o51010", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                new("o51200", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                new("o51210", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                // 추가
                new("o44010", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR),
                // FX
                new("g11004.AQ0901%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0904%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0906%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0908%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0910%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0911%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0914%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0920%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("g11004.AQ0923%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.TR, true),
                new("x00001", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                new("x00002", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                new("x00003", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                new("x00004", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                new("x00005", REQKIND_FUNC.CommFIDRqData, REQKIND_MASTER.조회, REQKIND_MAIN.해외, REQKIND_SUB.FID),
                // 공통
                new("g11004.AQ0495%", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.None, true),
                new("n51000", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.None),
                new("n51001", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.None),
                new("n51003", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.None),
                new("n51006", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.None),
                new("o44011", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.None),
                new("v90001", REQKIND_FUNC.CommRqData, REQKIND_MASTER.조회, REQKIND_MAIN.공통, REQKIND_SUB.None),

                // 실시간
                // 국내
                new("0051", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0052", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0058", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0059", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0065", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0066", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0071", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0073", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0075", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0077", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0078", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0079", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0056", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0068", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0101", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0310", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0120", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.시세),
                new("0181", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0182", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0183", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0184", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0185", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0211", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0212", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0213", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0261", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0262", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0265", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0271", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                new("0273", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.국내, REQKIND_SUB.주문),
                // 해외
                new("0076", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.시세),
                new("0082", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.시세),
                new("0241", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.시세),
                new("0242", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.시세),
                new("0196", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                new("0186", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                new("0188", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                new("0189", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                new("0190", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                new("0296", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                new("0286", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                new("0289", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.해외, REQKIND_SUB.주문),
                // FX
                new("0171", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.FX, REQKIND_SUB.시세),
                new("0197", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.FX, REQKIND_SUB.주문),
                new("0191", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.FX, REQKIND_SUB.주문),
                new("0192", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.FX, REQKIND_SUB.주문),
                new("0193", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.FX, REQKIND_SUB.주문),
                new("0194", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.FX, REQKIND_SUB.주문),
                new("0195", REQKIND_FUNC.CommSetJumunChe, REQKIND_MASTER.실시간, REQKIND_MAIN.FX, REQKIND_SUB.주문),
                // 공통
                new("-144", REQKIND_FUNC.None, REQKIND_MASTER.실시간, REQKIND_MAIN.공통, REQKIND_SUB.None),
                new("0161", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.공통, REQKIND_SUB.None),
                new("0208", REQKIND_FUNC.CommSetBroad, REQKIND_MASTER.실시간, REQKIND_MAIN.공통, REQKIND_SUB.None),
            ];

        private static REQKindClasses? GetPreDefineReq(string Code) => PreDefineReqs.FirstOrDefault(x => x.Code.Equals(Code));

        public static Task<IdTextItem> CreateAllTrFiles(IList<TRData> TrDatas)
        {
            return Task.Factory.StartNew(() =>
            {
                IdTextItem root = new(8, "전체TR파일")
                {
                    IsExpanded = true,
                };
                foreach (var trData in TrDatas)
                {
                    root.AddChild(new(7, $"{trData.FileTitle} : {trData.TRName}") { Tag = trData });
                }
                return root;
            });
        }

        public static Task<IdTextItem> CreateTrMainItem(REQKindClasses[] defReqs)
        {
            return Task.Factory.StartNew(() =>
            {
                IdTextItem root = new(0, "TR목록")
                {
                    IsExpanded = true,
                };
                var enum_Masters = Enum.GetNames(typeof(REQKIND_MASTER));
                var enum_Mains = Enum.GetNames(typeof(REQKIND_MAIN));
                var enum_Subs = Enum.GetNames(typeof(REQKIND_SUB));
                for (int i = 0; i < enum_Masters.Length; i++)
                {
                    var enum_master = (REQKIND_MASTER)i;
                    var masters = defReqs.Where(x => x.ReqKind_Master == enum_master);
                    if (masters.Count() == 0) continue;
                    IdTextItem lev_master = new(4, enum_Masters[i])
                    {
                        IsExpanded = true,
                    };
                    for (int j = 0; j < enum_Mains.Length; j++)
                    {
                        var enum_main = (REQKIND_MAIN)j;
                        var mains = masters.Where(x => x.ReqKind_Main == enum_main);
                        if (mains.Count() == 0) continue;
                        IdTextItem lev_main = new(11, enum_Mains[j]);
                        //lev_main.IsExpanded = true;
                        for (int k = 0; k < enum_Subs.Length; k++)
                        {
                            var enum_sub = (REQKIND_SUB)k;
                            var subs = mains.Where(x => x.ReqKind_Sub == enum_sub);
                            if (subs.Count() == 0) continue;
                            if (enum_sub == REQKIND_SUB.None)
                            {
                                foreach (var sub in subs)
                                {
                                    if (_codeToTrData.TryGetValue(sub.Code, out var trData))
                                    {
                                        lev_main.AddChild(new(7, $"{sub.Code} : {trData.TRName}") { Tag = trData });
                                    }
                                    else
                                        lev_main.AddChild(new(7, sub.Code));
                                }
                            }
                            else
                            {
                                IdTextItem lev_sub = new(8, enum_Subs[k]);
                                foreach (var sub in subs)
                                {
                                    if (_codeToTrData.TryGetValue(sub.Code, out var trData))
                                    {
                                        lev_sub.AddChild(new(7, $"{sub.Code} : {trData.TRName}") { Tag = trData });
                                    }
                                    else
                                        lev_sub.AddChild(new(7, sub.Code));
                                }
                                lev_main.AddChild(lev_sub);
                            }
                        }
                        lev_master.AddChild(lev_main);
                    }
                    root.AddChild(lev_master);
                }
                return root;
            });
        }

        public static Task<IdTextItem> CreateServiceMainItem(REQKindClasses[] defReqs)
        {
            return Task.Factory.StartNew(() =>
            {
                IdTextItem root = new(2, "서비스목록")
                {
                    IsExpanded = true,
                };
                var enum_Masters = Enum.GetNames(typeof(REQKIND_MASTER));
                var enum_Mains = Enum.GetNames(typeof(REQKIND_MAIN));
                var enum_Subs = Enum.GetNames(typeof(REQKIND_SUB));
                for (int j = 0; j < enum_Mains.Length; j++)
                {
                    var enum_main = (REQKIND_MAIN)j;
                    var mains = defReqs.Where(x => x.ReqKind_Main == enum_main);
                    if (mains.Count() == 0) continue;
                    IdTextItem lev_main = new(11, enum_Mains[j]);
                    //lev_main.IsExpanded = true;
                    for (int i = 0; i < enum_Masters.Length; i++)
                    {
                        var enum_master = (REQKIND_MASTER)i;
                        var masters = mains.Where(x => x.ReqKind_Master == enum_master);
                        if (masters.Count() == 0) continue;
                        IdTextItem lev_master = new(4, enum_Masters[i])
                        {
                            IsExpanded = true,
                        };
                        for (int k = 0; k < enum_Subs.Length; k++)
                        {
                            var enum_sub = (REQKIND_SUB)k;
                            var subs = masters.Where(x => x.ReqKind_Sub == enum_sub);
                            if (subs.Count() == 0) continue;
                            if (enum_sub == REQKIND_SUB.None)
                            {
                                foreach (var sub in subs)
                                {
                                    if (_codeToTrData.TryGetValue(sub.Code, out var trData))
                                    {
                                        lev_master.AddChild(new(7, $"{sub.Code} : {trData.TRName}") { Tag = trData });
                                    }
                                    else
                                        lev_master.AddChild(new(7, sub.Code));
                                }
                            }
                            else
                            {
                                IdTextItem lev_sub = new(8, enum_Subs[k]);
                                foreach (var sub in subs)
                                {
                                    if (_codeToTrData.TryGetValue(sub.Code, out var trData))
                                    {
                                        lev_sub.AddChild(new(7, $"{sub.Code} : {trData.TRName}") { Tag = trData });
                                    }
                                    else
                                        lev_sub.AddChild(new(7, sub.Code));
                                }
                                lev_master.AddChild(lev_sub);
                            }
                        }
                        lev_main.AddChild(lev_master);
                    }
                    root.AddChild(lev_main);
                }
                return root;
            });
        }

        public class SectionGroup
        {
            public string Section;
            public IDictionary<string, string> KeyValues;
            public SectionGroup(string Section)
            {
                this.Section = Section;
                KeyValues = new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        public static Task<IdTextItem> CreateInstanceFuncs(object instance)
        {
            return Task.Factory.StartNew(() =>
            {
                IdTextItem root = new(3, "함수목록") { IsExpanded = true };
                Type intanceType = instance.GetType();
                var methods = intanceType.GetMethods();

                foreach (var method in methods)
                {
                    if (method.IsVirtual && method.IsSecurityCritical)
                    {
                        IdTextItem iconTextItem = new(8, method.Name) { Tag = method.Name };
                        root.AddChild(iconTextItem);
                    }
                }

                return root;
            });
        }
    }


}
