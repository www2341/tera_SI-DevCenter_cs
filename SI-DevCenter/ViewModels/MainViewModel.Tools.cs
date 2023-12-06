using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SI.Component.Components;
using SI.Component.Models;
using SI_DevCenter.Models;
using SI_DevCenter.Models.SI;
using SI_DevCenter.Views;
using StockDevControl.StockModels;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    public List<string> MenuCustomizeItems { get; } =
        [
            "SI증권 홈페이지",
            "국내 오류코드",
            "해외 오류코드",
            "OutRec",
#if DEBUG
            "국내선물 마스터파일",
            "해외선물 마스터파일",
#endif
        ];

    [RelayCommand]
    void MenuCustomize(string text)
    {
        if (text.Equals("SI증권 홈페이지"))
        {
            var sInfo = new System.Diagnostics.ProcessStartInfo("https://www.si-sec.com/")
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }
        else if (text.Equals("국내 오류코드"))
        {
            string filepath = _apiFolderPath + "\\Data\\국내_uamsg.dat";
            string result;
            try
            {
                result = File.ReadAllText(filepath, _krEncoder);
            }
            catch (Exception)
            {
                result = $"파일을 읽을 수 없습니다: {filepath}";
            }
            UserContent = null;
            _propertyTarget = null;
            ResultPath = string.Empty;
            SetResultText(result);
        }
        else if (text.Equals("해외 오류코드"))
        {
            string filepath = _apiFolderPath + "\\Data\\해외_uamsg.dat";
            string result;
            try
            {
                result = File.ReadAllText(filepath, _krEncoder);
            }
            catch (Exception)
            {
                result = $"파일을 읽을 수 없습니다: {filepath}";
            }
            UserContent = null;
            _propertyTarget = null;
            ResultPath = string.Empty;
            SetResultText(result);
        }
        else if (text.Equals("OutRec"))
        {
            if (_propertyTarget is TRData trData)
            {
                StringBuilder sb = new StringBuilder();

                // OutRec1
                {
                    sb.AppendLine($"internal class STRUCT_{trData.TRCode}_OutRec1");
                    sb.AppendLine("{");
                    sb.AppendLine("    public enum Kind");
                    sb.AppendLine("    {");
                    foreach (var name in trData.OutRec1Names)
                    {
                        sb.AppendLine($"        {name},");
                    }
                    sb.AppendLine("    }");
                    int[] per_sizes = trData.OutRec1Sizes.ToArray();

                    sb.AppendLine("    public static readonly int[] FieldIndexs =");
                    sb.AppendLine("        [");
                    sb.Append("            ");
                    int sum_size = 0;
                    foreach (var data in per_sizes)
                    {
                        sb.Append($"{sum_size}, ");
                        sum_size += data;
                    }
                    sb.AppendLine();
                    sb.AppendLine("        ];");

                    sb.AppendLine("    public static readonly int[] FieldSizes =");
                    sb.AppendLine("        [");
                    sb.Append("            ");
                    foreach (var size in per_sizes)
                        sb.Append($"{size}, ");
                    sb.AppendLine();
                    sb.AppendLine("        ];");

                    sb.AppendLine($"    public static readonly int FrameSize = {sum_size};");

                    sb.AppendLine("}");
                }

                sb.AppendLine();

                // OutRec2
                {
                    sb.AppendLine($"internal class STRUCT_{trData.TRCode}_OutRec2");
                    sb.AppendLine("{");
                    sb.AppendLine("    public enum Kind");
                    sb.AppendLine("    {");
                    foreach (var name in trData.OutRec2Names)
                    {
                        sb.AppendLine($"        {name},");
                    }
                    sb.AppendLine("    }");
                    int[] per_sizes = trData.OutRec2Sizes.ToArray();

                    sb.AppendLine("    public static readonly int[] FieldIndexs =");
                    sb.AppendLine("        [");
                    sb.Append("            ");
                    int sum_size = 0;
                    foreach (var data in per_sizes)
                    {
                        sb.Append($"{sum_size}, ");
                        sum_size += data;
                    }
                    sb.AppendLine();
                    sb.AppendLine("        ];");

                    sb.AppendLine("    public static readonly int[] FieldSizes =");
                    sb.AppendLine("        [");
                    sb.Append("            ");
                    foreach (var size in per_sizes)
                        sb.Append($"{size}, ");
                    sb.AppendLine();
                    sb.AppendLine("        ];");

                    sb.AppendLine($"    public static readonly int FrameSize = {sum_size};");

                    sb.AppendLine("}");
                }


                _propertyTarget = null;
                ResultPath = string.Empty;
                SetResultText(sb.ToString());

                ResultSaveCommand.NotifyCanExecuteChanged();
            }
            else
            {
                OutputLog(LogKind.LOGS, "TR선택 후 요청해 주세요");
            }
        }
        else if (text.Equals("해외선물 마스터파일"))
        {
            if (_hw_JMCodes.Count == 0)
            {
                AddTreeItemInfos_HW_JMCode();
            }
        }
        else if (text.Equals("국내선물 마스터파일"))
        {
            if (_kr_JMCodes.Count == 0)
            {
                AddTreeItemInfos_KR_Futcode();
            }
        }
    }

    [ObservableProperty]
    bool _IsUserContentVisibled;

    object? _userContent;
    public object? UserContent
    {
        get => _userContent;
        set
        {
            if (_userContent != value)
            {
                if (_userContent is IUserTool userTool)
                {
                    userTool.CloseTool();
                }
                _userContent = value;
                OnPropertyChanged();
                IsUserContentVisibled = _userContent != null;
            }
        }
    }

    private readonly KRFutureOrderModel _krFutureOrderModel = new KRFutureOrderModel();
    private readonly HWFutureOrderModel _hwFutureOrderModel = new HWFutureOrderModel();

    private readonly ChartReqModel _krFutureChartModel;
    private readonly ChartReqModel _hwFutureChartModel;
    bool ProcUserTool(string toolName)
    {
        object? userContent = null;
        if (toolName.Equals("국내선물주문") || toolName.Equals("해외선물주문"))
        {
            bool b국내 = toolName.Equals("국내선물주문");
            // 계좌리스트, 종목 리스트 업데이트
            FutureOrderModel orderModel = b국내 ? _krFutureOrderModel : _hwFutureOrderModel;
            orderModel.계좌리스트 = _axHDFCommAgent.AccountInfos.Where(x => x.계좌구분 == (b국내 ? AccountInfo.KIND.국내 : AccountInfo.KIND.해외)).ToList();

            if (orderModel.Selected계좌 == null && orderModel.계좌리스트.Count > 0)
                orderModel.Selected계좌 = orderModel.계좌리스트[0];

            orderModel.종목리스트 = b국내 ? _axHDFCommAgent.KRFutureItemInfos : _axHDFCommAgent.HWFutureKwanItemInfos;
            if (orderModel.Selected종목 == null && orderModel.종목리스트.Count > 0)
            {
                if (orderModel.Selected계좌 != null)
                {
                    string 종목코드 = _appRegistry.GetValue(orderModel.Selected계좌.계좌번호, "종목코드", string.Empty);
                    orderModel.Selected종목 = _axHDFCommAgent.GetItemInfo(종목코드);
                }

                if (orderModel.Selected종목 == null)
                    orderModel.Selected종목 = orderModel.종목리스트[0];
            }

            if (orderModel.비밀번호.Length == 0 && _serverType == SERVER_TYPE.Simulation)
            {
                orderModel.비밀번호 = "1234";
            }

            userContent = new FutureOrderComponent(toolName, orderModel, OrderViewReqCommand)
            {
            };
        }
        else if (toolName.Equals("국내선물 차트요청") || toolName.Equals("해외선물 차트요청"))
        {
            bool b국내 = toolName.Equals("국내선물 차트요청");
            ChartReqModel chartModel = b국내 ? _krFutureChartModel : _hwFutureChartModel;

            chartModel.종목리스트 = b국내 ? _axHDFCommAgent.KRFutureItemInfos : _axHDFCommAgent.HWFutureKwanItemInfos;
            if (chartModel.Selected종목 == null && chartModel.종목리스트.Count > 0)
            {
                string 종목코드 = _appRegistry.GetValue(chartModel.Title, "종목코드", string.Empty);
                chartModel.Selected종목 = _axHDFCommAgent.GetItemInfo(종목코드);

                if (chartModel.Selected종목 == null)
                    chartModel.Selected종목 = chartModel.종목리스트[0];
            }

            chartModel.CodeText = MakeChartReqCode(chartModel);

            userContent = new ChartReqComponent(chartModel);
        }
        UserContent = userContent;
        return IsUserContentVisibled;
    }

    void SaveToolsData()
    {
        if (_krFutureOrderModel.Selected계좌 != null && _krFutureOrderModel.Selected종목 != null)
        {
            _appRegistry.SetValue(_krFutureOrderModel.Selected계좌.계좌번호, "종목코드", _krFutureOrderModel.Selected종목.종목코드);
        }
        if (_hwFutureOrderModel.Selected계좌 != null && _hwFutureOrderModel.Selected종목 != null)
        {
            _appRegistry.SetValue(_hwFutureOrderModel.Selected계좌.계좌번호, "종목코드", _hwFutureOrderModel.Selected종목.종목코드);
        }

        if (_krFutureChartModel.Selected종목 != null)
        {
            _appRegistry.SetValue(_krFutureChartModel.Title, "종목코드", _krFutureChartModel.Selected종목.종목코드);
        }
        if (_hwFutureChartModel.Selected종목 != null)
        {
            _appRegistry.SetValue(_hwFutureChartModel.Title, "종목코드", _hwFutureChartModel.Selected종목.종목코드);
        }
        _appRegistry.SetValue(_krFutureChartModel.Title, "ChartRound", _krFutureChartModel.SelectedChartRound);
        _appRegistry.SetValue(_krFutureChartModel.Title, "ChartInterval", _krFutureChartModel.SelectedChartInterval);
        _appRegistry.SetValue(_krFutureChartModel.Title, "DataCount", _krFutureChartModel.SelectedDataCount);

        _appRegistry.SetValue(_hwFutureChartModel.Title, "ChartRound", _hwFutureChartModel.SelectedChartRound);
        _appRegistry.SetValue(_hwFutureChartModel.Title, "ChartInterval", _hwFutureChartModel.SelectedChartInterval);
        _appRegistry.SetValue(_hwFutureChartModel.Title, "DataCount", _hwFutureChartModel.SelectedDataCount);

    }

    private string GetHWFutureJumunSvrInputText(FutureOrderModel model, bool b정정주문 = false)
    {
        var 매매구분 = model.매매구분;
        var 지정구분 = (HWFutureOrderModel.OrderSpecMain)model.지정구분;
        var 체결구분 = (HWFutureOrderModel.OrderSpecSub)model.체결구분;

        string 계좌번호 = string.Empty;
        if (model.Selected계좌 != null) 계좌번호 = model.Selected계좌.계좌번호;

        string 종목코드 = string.Empty;
        if (model.Selected종목 != null) 종목코드 = model.Selected종목.종목코드;
        string 주문가격 = "0";
        if (지정구분 != HWFutureOrderModel.OrderSpecMain.시장가)
            주문가격 = _axHDFCommAgent.CommGetHWOrdPrice(종목코드, model.주문가격.ToString(), 2);
        string STOP가격 = string.Empty;
        if (지정구분 == HWFutureOrderModel.OrderSpecMain.STOP || 지정구분 == HWFutureOrderModel.OrderSpecMain.STOP지정가)
            STOP가격 = _axHDFCommAgent.CommGetHWOrdPrice(종목코드, model.STOP가격.ToString(), 2);
        string IOC최소체결수량 = string.Empty;
        if (체결구분 == HWFutureOrderModel.OrderSpecSub.IOC)
            IOC최소체결수량 = model.IOC수량.ToString();
        string 주문유효일자 = string.Empty;
        if (체결구분 == HWFutureOrderModel.OrderSpecSub.GTD)
            주문유효일자 = model.GTDDate.ToString("yyyyMMdd");

        string result = string.Empty;

        if (매매구분 == OrderType.매수 || 매매구분 == OrderType.매도)
        {
            // g12003.AO0401%
            result = $"{계좌번호,-6}{model.비밀번호,-8}{종목코드,-32}{(int)매매구분,-1}{(int)지정구분,-1}{(int)체결구분,-1}{주문가격,-15}{model.주문수량,-10}1{STOP가격,-15}{IOC최소체결수량,10}N{주문유효일자,-8}";

            // 사용자필드는 생략
        }
        else if (매매구분 == OrderType.정정취소)
        {
            if (b정정주문) // 해외주문 정정
            {
                // g12003.AO0402%
                result = $"{계좌번호,-6}{model.비밀번호,-8}{종목코드,-32}{(int)지정구분,-1}{(int)체결구분,-1}{주문가격,-15}{model.주문수량,-10}{model.주문번호,-10}1{STOP가격,-15}{IOC최소체결수량,10}";

                // 사용자필드는 생략
            }
            else // 해외주문 취소
            {
                // g12003.AO0403%: 데이터구조는 g12003.AO0402% 와 같음
                result = $"{계좌번호,-6}{model.비밀번호,-8}{종목코드,-32}{(int)지정구분,-1} {"0",-15}{string.Empty,-10}{model.주문번호,-10} {string.Empty,-15}{string.Empty,10}";

                // 사용자필드는 생략
            }
        }

        return result;
    }

    private string GetKRFutureJumunSvrInputText(FutureOrderModel model, bool b정정주문 = false)
    {
        var 매매구분 = model.매매구분;
        var 지정구분 = (KRFutureOrderModel.OrderSpecMain)model.지정구분;
        var 체결구분 = (KRFutureOrderModel.OrderSpecSub)model.체결구분;

        string 계좌번호 = string.Empty;
        if (model.Selected계좌 != null) 계좌번호 = model.Selected계좌.계좌번호;

        string 종목코드 = string.Empty;
        if (model.Selected종목 != null) 종목코드 = model.Selected종목.종목코드;
        string 주문가격 = "0";
        if (지정구분 != KRFutureOrderModel.OrderSpecMain.시장가)
            주문가격 = model.주문가격.ToString();

        string result = string.Empty;
        if (매매구분 == OrderType.매수 || 매매구분 == OrderType.매도)
        {
            // g12001.DO1601&
            result = $"{계좌번호,-11}{model.비밀번호,-8}{종목코드,-32}{(int)매매구분,-1}{(int)지정구분,-1}{(int)체결구분,-1}{주문가격,-13}{model.주문수량,-5}";

            // 사용자필드는 생략
        }
        else if (매매구분 == OrderType.정정취소)
        {
            if (b정정주문) // 국내주문 정정
            {
                // g12001.DO1901&
                result = $"{계좌번호,-11}{model.비밀번호,-8}{종목코드,-32} {(int)지정구분,-1}{(int)체결구분,-1}{주문가격,-13}{model.주문수량,-5}{model.주문번호,-7}";
                // 사용자필드는 생략
            }
            else // 국내주문 취소
            {
                // g12001.DO1701&: 데이터구조는 g12003.AO0402% 와 같음
                result = $"{계좌번호,-11}{model.비밀번호,-8}{종목코드,-32} {(int)지정구분,-1}{(int)체결구분,-1}{0,-13}{" ",-5}{model.주문번호,-7}";

                // 사용자필드는 생략
            }
        }
        return result;
    }

    private string OrderViewReqCommand(FutureOrderComponent sendor, string action)
    {
        string result = string.Empty;
        var orderModel = sendor.OrderModel;

        if (action.Equals("변환요청"))
        {
            StringBuilder sb = new StringBuilder();

            if (orderModel is HWFutureOrderModel) // 해외선물
            {
                if (orderModel.매매구분 == OrderType.매수 || orderModel.매매구분 == OrderType.매도)
                {
                    if (orderModel.매매구분 == OrderType.매수)
                        sb.AppendLine("// 해외선물 신규 매수주문");
                    else
                        sb.AppendLine("// 해외선물 신규 매도주문");

                    sb.AppendLine("string sTrCode = \"g12003.AO0401%\";");
                    sb.AppendLine($"string sInput = \"{GetHWFutureJumunSvrInputText(orderModel, false)}\";");
                    sb.AppendLine("int nRqID = m_CommAgent.CommJumunSvr(sTrCode,  sInput);");
                }
                else
                {
                    // 정정주문
                    sb.AppendLine("// 해외선물 정정주문");
                    sb.AppendLine("string sTrCode = \"g12003.AO0402%\";");
                    sb.AppendLine($"string sInput = \"{GetHWFutureJumunSvrInputText(orderModel, true)}\";");
                    sb.AppendLine("int nRqID = m_CommAgent.CommJumunSvr(sTrCode,  sInput);");
                    sb.AppendLine();

                    // 취소주문
                    sb.AppendLine("// 해외선물 취소주문");
                    sb.AppendLine("string sTrCode = \"g12003.AO0403%\";");
                    sb.AppendLine($"string sInput = \"{GetHWFutureJumunSvrInputText(orderModel, false)}\";");
                    sb.AppendLine("int nRqID = m_CommAgent.CommJumunSvr(sTrCode,  sInput);");
                }
            }
            else if (orderModel is KRFutureOrderModel)
            {
                if (orderModel.매매구분 == OrderType.매수 || orderModel.매매구분 == OrderType.매도)
                {
                    if (orderModel.매매구분 == OrderType.매수)
                        sb.AppendLine("// 국내선물 신규 매수주문");
                    else
                        sb.AppendLine("// 국내선물 신규 매도주문");

                    sb.AppendLine("string sTrCode = \"g12001.DO1601&\";");
                    sb.AppendLine($"string sInput = \"{GetKRFutureJumunSvrInputText(orderModel, false)}\";");
                    sb.AppendLine("int nRqID = m_CommAgent.CommJumunSvr(sTrCode,  sInput);");
                }
                else
                {
                    // 정정주문
                    sb.AppendLine("// 국내선물 정정주문");
                    sb.AppendLine("string sTrCode = \"g12001.DO1901&\";");
                    sb.AppendLine($"string sInput = \"{GetKRFutureJumunSvrInputText(orderModel, true)}\";");
                    sb.AppendLine("int nRqID = m_CommAgent.CommJumunSvr(sTrCode,  sInput);");
                    sb.AppendLine();

                    // 취소주문
                    sb.AppendLine("// 국내선물 취소주문");
                    sb.AppendLine("string sTrCode = \"g12001.DO1701&\";");
                    sb.AppendLine($"string sInput = \"{GetKRFutureJumunSvrInputText(orderModel, false)}\";");
                    sb.AppendLine("int nRqID = m_CommAgent.CommJumunSvr(sTrCode,  sInput);");
                }
            }
            return sb.ToString();
        }
        if (_loginState != Models.LoginState.LOGINED)
        {
            OutputLog(LogKind.LOGS, "로그인 후 요청해 주세요");
            return result;
        }
        var 계좌 = orderModel.Selected계좌;
        if (계좌 == null)
        {
            OutputLog(LogKind.LOGS, "선택된 계좌가 없습니다.");
            return result;
        }

        if (orderModel is HWFutureOrderModel) // 해외선물 주문모델
        {
            if (action.Equals("실시간현재가요청"))
            {
                if (sendor.JangoItems != null)
                {
                    foreach (var item in sendor.JangoItems)
                        _axHDFCommAgent.CommSetBroad(item.종목정보.종목코드, 0082);
                }
            }
            else if (action.Equals("잔고요청"))
            {
                // g11004.AQ0403% : 해외 미결제내역조회
                계좌.JangoItems.Clear();
                string sTrCode = "g11004.AQ0403%";
                string sInput = $"1{_axHDFCommAgent.LoginInfo.UserID,-8}{계좌.계좌번호,-6}{orderModel.비밀번호,-8}{string.Empty,20}";
                int nrqld = _axHDFCommAgent.CommRqData(sTrCode, sInput, sInput.Length, string.Empty);
                if (nrqld > 0)
                {
                    계좌.JangoInited = true;
                }
            }
            else if (action.Equals("미체결요청"))
            {
                // g11004.AQ0401% :해외 미체결내역조회
                string sTrCode = "g11004.AQ0401%";
                string sInput = $"1{_axHDFCommAgent.LoginInfo.UserID,-8}{계좌.계좌번호,-6}{orderModel.비밀번호,-8}{string.Empty,20}";
                _axHDFCommAgent.CommRqData(sTrCode, sInput, sInput.Length, string.Empty);
            }
            else // 주문요청
            {
                bool b매수주문 = orderModel.매매구분 == OrderType.매수;
                bool b매도주문 = orderModel.매매구분 == OrderType.매도;
                bool b정정주문 = orderModel.매매구분 == OrderType.정정취소 && action.Equals("정정주문");
                bool b취소주문 = orderModel.매매구분 == OrderType.정정취소 && (b정정주문 == false);

                var 종목 = orderModel.Selected종목;
                if (종목 == null)
                {
                    OutputLog(Models.LogKind.LOGS, "선택된 종목이 없습니다.");
                    return result;
                }

                string sTrCode = string.Empty;
                if (b매수주문 || b매도주문)
                {
                    sTrCode = "g12003.AO0401%"; // 해외주문 신규
                }
                else if (orderModel.매매구분 == OrderType.정정취소)
                {
                    if (b정정주문)
                        sTrCode = "g12003.AO0402%"; // 해외주문 정정
                    else
                        sTrCode = "g12003.AO0403%"; // 해외주문 취소
                }

                string sInputs = GetHWFutureJumunSvrInputText(orderModel, b정정주문);
                if (sInputs.Length == 0)
                {
                    OutputLog(Models.LogKind.LOGS, "입력 데이터가 없습니다.");
                    return result;
                }

                // 주문요청 확인창
                if (orderModel.주문확인생략 == false)
                {
                    string caption = action + " 확인창";
                    string msg = $"{종목.종목명}\r\n{orderModel.주문수량}계약\r\n{action} 하시겠습니까?";
                    var dlg = new MessageOkCancel(msg, caption);
                    if (dlg.ShowDialog() == false) return result;
                }

                OutputLog(Models.LogKind.LOGS, $"계좌: {계좌.계좌번호} : [{종목.종목코드}] : {orderModel.주문수량}계약 {orderModel.지정구분} {action} 요청");

                Stopwatch stopwatch = Stopwatch.StartNew();
                int nRqID = _axHDFCommAgent.CommJumunSvr(sTrCode, sInputs);
                stopwatch.Stop();
                var Now = DateTime.Now;
                result = $"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS : nRqID = {nRqID}\r\n";

                if (nRqID > 1)
                {
                    OutputLog(Models.LogKind.LOGS, $"주문정상({nRqID})");
                }
                else
                {
                    OutputLog(Models.LogKind.LOGS, $"주문오류({nRqID})");
                }

            }
        }
        else if (orderModel is KRFutureOrderModel)
        {
            if (action.Equals("실시간현재가요청"))
            {
                if (sendor.JangoItems != null)
                {
                    foreach (var item in sendor.JangoItems)
                        _axHDFCommAgent.CommSetBroad(item.종목정보.종목코드, 0065);
                }
            }
            else if (action.Equals("잔고요청"))
            {
                // g11002.DQ1305& : 국내 미결제내역조회(평가손익)
                계좌.JangoItems.Clear();
                string sTrCode = "g11002.DQ1305&";
                string sInput = $"{계좌.계좌번호,-11}001{orderModel.비밀번호,-8}";
                int nrqld = _axHDFCommAgent.CommRqData(sTrCode, sInput, sInput.Length, string.Empty);
                if (nrqld > 0)
                {
                    계좌.JangoInited = true;
                }
            }
            else if (action.Equals("미체결요청"))
            {
                // g11002.DQ0104& :국내 미체결내역조회
                string sTrCode = "g11002.DQ0104&";
                string sInput = $"{계좌.계좌번호,-11}001{orderModel.비밀번호,-8}";
                _axHDFCommAgent.CommRqData(sTrCode, sInput, sInput.Length, string.Empty);
            }
            else // 주문요청
            {
                bool b매수주문 = orderModel.매매구분 == OrderType.매수;
                bool b매도주문 = orderModel.매매구분 == OrderType.매도;
                bool b정정주문 = orderModel.매매구분 == OrderType.정정취소 && action.Equals("정정주문");
                bool b취소주문 = orderModel.매매구분 == OrderType.정정취소 && (b정정주문 == false);

                var 종목 = orderModel.Selected종목;
                if (종목 == null)
                {
                    OutputLog(Models.LogKind.LOGS, "선택된 종목이 없습니다.");
                    return result;
                }

                string sTrCode = string.Empty;
                if (b매수주문 || b매도주문)
                {
                    sTrCode = "g12001.DO1601&"; // 국내주문 신규
                }
                else if (orderModel.매매구분 == OrderType.정정취소)
                {
                    if (b정정주문)
                        sTrCode = "g12001.DO1901&"; // 국내주문 정정
                    else
                        sTrCode = "g12001.DO1701&\t"; // 국내주문 취소
                }

                string sInputs = GetKRFutureJumunSvrInputText(orderModel, b정정주문);
                if (sInputs.Length == 0)
                {
                    OutputLog(Models.LogKind.LOGS, "입력 데이터가 없습니다.");
                    return result;
                }

                // 주문요청 확인창
                if (orderModel.주문확인생략 == false)
                {
                    string caption = action + " 확인창";
                    string msg = $"{종목.종목명}\r\n{orderModel.주문수량}계약\r\n{action} 하시겠습니까?";
                    var dlg = new MessageOkCancel(msg, caption);
                    if (dlg.ShowDialog() == false) return result;
                }

                OutputLog(Models.LogKind.LOGS, $"계좌: {계좌.계좌번호} : [{종목.종목코드}] : {orderModel.주문수량}계약 {orderModel.지정구분} {action} 요청");

                Stopwatch stopwatch = Stopwatch.StartNew();
                int nRqID = _axHDFCommAgent.CommJumunSvr(sTrCode, sInputs);
                stopwatch.Stop();
                var Now = DateTime.Now;
                result = $"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS : nRqID = {nRqID}\r\n";

                if (nRqID > 1)
                {
                    OutputLog(Models.LogKind.LOGS, $"주문정상({nRqID})");
                }
                else
                {
                    OutputLog(Models.LogKind.LOGS, $"주문오류({nRqID})");
                }
            }
        }

        return result;
    }

    private void ChartReqCommand(ChartReqModel chartReqModel, string action_name)
    {
        if (_loginState != Models.LoginState.LOGINED)
        {
            OutputLog(LogKind.LOGS, "로그인 후 요청해 주세요");
            return;
        }

        if (action_name.Equals("조 회"))
        {
            bool b국내 = chartReqModel.GroupKind == AccountInfo.KIND.국내;

            string sTrCode = b국내 ? "v90003" : "o44005";
            string sInput = GetChartReqInputs(chartReqModel);

            string strNextKey = string.Empty;

            Stopwatch stopwatch = Stopwatch.StartNew();
            int nRqID = _axHDFCommAgent.CommRqData(sTrCode, sInput, sInput.Length, strNextKey);
            stopwatch.Stop();
            var Now = DateTime.Now;
            string result = $"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS : nRqID = {nRqID}\r\n";
            chartReqModel.NRqId = nRqID;

            if (nRqID > 1)
            {
                OutputLog(Models.LogKind.LOGS, $"요청정상({nRqID})");
            }
            else
            {
                OutputLog(Models.LogKind.LOGS, $"요청오류({nRqID})");
            }

            chartReqModel.CodeText += result;
        }
    }

    private string MakeChartReqCode(ChartReqModel chartReqModel)
    {
        bool b국내 = chartReqModel.GroupKind == AccountInfo.KIND.국내;

        string sTrCode = b국내 ? "v90003" : "o44005";
        string sInputs = GetChartReqInputs(chartReqModel);

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine(b국내 ? "// 국내선물 차트조회 (날짜시간, 시가, 고가, 저가, 종가, 거래량)" : "// 해외선물 차트조회 (국내일자, 국내시간, 시가, 고가, 저가, 종가, 체결량)");
        stringBuilder.AppendLine($"string sTrCode = \"{sTrCode}\";");
        stringBuilder.AppendLine($"string sInput = \"{sInputs}\";");
        stringBuilder.AppendLine($"string strNextKey = string.Empty;");
        stringBuilder.AppendLine($"int nRqID = m_CommAgent.CommRqData(sTrCode,  sInput, sInput.Length, strNextKey);");

        return stringBuilder.ToString();
    }

    private string GetChartReqInputs(ChartReqModel chartReqModel)
    {
        bool b국내 = chartReqModel.GroupKind == AccountInfo.KIND.국내;

        StringBuilder sInputBuilder = new();

        if (b국내)
        {
            sInputBuilder.Append($"{(chartReqModel.Selected종목 != null ? chartReqModel.Selected종목.종목코드 : string.Empty),-15}");
            sInputBuilder.Append("99999999"); // 일자
            sInputBuilder.Append("999999"); // 6자리

            int.TryParse(chartReqModel.SelectedDataCount, out int 조회건수);
            if (조회건수 < 1) 조회건수 = 1;
            if (조회건수 > 9999) 조회건수 = 9999;
            sInputBuilder.Append($"{조회건수:d4}");

            int.TryParse(chartReqModel.SelectedChartInterval, out int N봉);
            if (N봉 < 1) N봉 = 1;
            if (N봉 > 999) N봉 = 999;
            if (chartReqModel.SelectedChartRound == ChartRound.일) N봉 = 1;
            sInputBuilder.Append($"{N봉:d3}");

            int 구분 = chartReqModel.SelectedChartRound switch
            {
                ChartRound.일 => 2,
                ChartRound.분 => 1,
                ChartRound.틱 => 0,
                _ => 2
            };
            sInputBuilder.Append($"{구분,-1}");
            sInputBuilder.Append("0"); // 0:최초, 1:다음
            sInputBuilder.Append($"{string.Empty,-21}"); // 조회응답시 수신받은 "이전키"값
            sInputBuilder.Append("00000000000 ");
            sInputBuilder.Append("0"); // 연속월물조회 0:사용안함, 1:사용
        }
        else
        {
            sInputBuilder.Append($"{string.Empty,-18}"); // 최초 조회시 무조건 공백, 다음 조회시 응답부의 keyvalue를 이용.
            sInputBuilder.Append($"{(chartReqModel.Selected종목 != null ? chartReqModel.Selected종목.종목코드 : string.Empty),-32}");
            sInputBuilder.Append("99999999"); // 일자
            sInputBuilder.Append("9999999999"); // 시간
            sInputBuilder.Append("0"); // 0:최초, 1:다음

            int 구분 = chartReqModel.SelectedChartRound switch
            {
                ChartRound.일 => 3,
                ChartRound.분 => 2,
                ChartRound.틱 => 6,
                _ => 3
            };
            sInputBuilder.Append($"{구분,-1}");

            int.TryParse(chartReqModel.SelectedChartInterval, out int N봉);
            if (N봉 < 1) N봉 = 1;
            if (N봉 > 999) N봉 = 999;
            if (chartReqModel.SelectedChartRound == ChartRound.일) N봉 = 1;
            sInputBuilder.Append($"{N봉:d3}");

            int.TryParse(chartReqModel.SelectedDataCount, out int 조회건수);
            if (조회건수 < 1) 조회건수 = 1;
            if (조회건수 > 99999) 조회건수 = 99999;
            sInputBuilder.Append($"{조회건수:d5}");
            sInputBuilder.Append("1"); // 1:전산장, 2:본장
            sInputBuilder.Append("1"); // 1: 실봉만, 0:허봉+실봉
            //if (chartReqModel.SelectedTrCode.Equals("o44005"))
            //{
            //}
            //else if (chartReqModel.SelectedTrCode.Equals("o44010"))
            //{
            //    sInputBuilder.Append($"{(chartReqModel.Selected종목 != null ? chartReqModel.Selected종목.종목코드 : string.Empty),-32}");
            //    sInputBuilder.Append("1"); // 사용모드
            //    sInputBuilder.Append("1"); // 조회구분 1: 틱, 2: 분, 3: 일
            //    int.TryParse(chartReqModel.SelectedChartInterval, out int N봉);
            //    if (N봉 < 1) N봉 = 1;
            //    if (N봉 > 999) N봉 = 999;
            //    if (chartReqModel.SelectedChartRound == ChartRound.일) N봉 = 1;
            //    sInputBuilder.Append($"{N봉:d3}");
            //    sInputBuilder.Append("1"); // 조회유형
            //    int.TryParse(chartReqModel.SelectedDataCount, out int 조회건수);
            //    if (조회건수 < 1) 조회건수 = 1;
            //    if (조회건수 > 99999) 조회건수 = 99999;
            //    sInputBuilder.Append($"{조회건수:d5}");
            //    sInputBuilder.Append("99999999"); // 시작일자
            //    sInputBuilder.Append("9999999999"); // 시작시간
            //    sInputBuilder.Append("99999999"); // 종료일자
            //    sInputBuilder.Append("9999999999"); // 종료시간
            //    sInputBuilder.Append("0"); // 1: 실봉만, 0:허봉+실봉
            //    sInputBuilder.Append($"{string.Empty,32}"); // 차트사용데이터
            //    sInputBuilder.Append("N"); // 연속구분
            //}
        }

        return sInputBuilder.ToString();
    }
}

