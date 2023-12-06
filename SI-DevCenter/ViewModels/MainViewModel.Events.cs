using HDFCommAgent.NET;
using SI_DevCenter.Models;
using SI_DevCenter.Models.StructModels;
using SI_DevCenter.Repositories;
using StockDevControl.StockModels;
using System.IO;
using System.Text;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    private void _axHDFCommAgent_OnGetMsgWithRqId(object sender, _DHDFCommAgentEvents_OnGetMsgWithRqIdEvent e)
    {
        string msg = $"nRqId={e.nRqId}, sCode={e.sCode}, sMsg={e.sMsg}";
        OutputLog(LogKind.OnGetMsgWithRqId, msg);

        if (e.sMsg.IndexOf("[MST] 해외종목 메모리 로드 완료") == 0)
        {
            if (_hw_JMCodes.Count == 0)
            {
                AddTreeItemInfos_HW_JMCode();
            }
        }
    }

    //private void _axHDFCommAgent_OnGetMsg(object sender, _DHDFCommAgentEvents_OnGetMsgEvent e)
    //{
    //    string msg = $"sCode={e.sCode}, sMsg={e.sMsg}";
    //    OutputLog(LogKind.OnGetMsg, msg);
    //}

    private void _axHDFCommAgent_OnGetBroadData(object sender, _DHDFCommAgentEvents_OnGetBroadDataEvent e)
    {
        TRData? trData = HDFTrManager.GetTRData(e.nRealType);
        if (trData != null)
        {
            int size1 = trData.OutRec1TotalSize;
            string recv_text = _axHDFCommAgent.CommGetDataDirect(e.sJongmokCode, e.nRealType, 0, size1, 0, "A");

            string msg = $"sJongmokCode={e.sJongmokCode}, nRealType={e.nRealType:d4}, OutRec1Data[{size1:d3}]={recv_text}";
            OutputLog(LogKind.OnGetBroadData, msg);

            // 국내주문미체결 (사용자정의필드)
            if (e.nRealType == 0262)
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "계좌번호").Trim();
                var accountInfo = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (accountInfo != null)
                {
                    string 종목코드 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "종목코드").Trim();
                    var item_info = _axHDFCommAgent.GetItemInfo(종목코드);
                    if (item_info != null)
                    {
                        string 주문번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "주문번호").Trim();
                        string 원주문번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "최초원주문번호").Trim();
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "주문수량").Trim(), out int 주문수량);
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "체결수량").Trim(), out int 체결수량);
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "잔량").Trim(), out int 잔량);
                        var exist_item = accountInfo.MicheItems.FirstOrDefault(x => x.주문번호.Equals(주문번호));
                        OrderType 매매구분 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "매매구분").Trim().Equals("1") ? OrderType.매수 : OrderType.매도;
                        double 주문가격 = double.Parse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "주문가격").Trim());
                        double STOP가격 = 0;
                        if (exist_item != null)
                        {
                            if (잔량 == 0)
                                accountInfo.MicheItems.Remove(exist_item);
                            else
                            {
                                exist_item.주문수량 = 주문수량;
                                exist_item.주문잔량 = 잔량;
                                exist_item.주문가격 = 주문가격;
                                exist_item.STOP가격 = STOP가격;
                            }
                        }
                        else
                        {
                            if (잔량 > 0)
                            {
                                var new_item = new MicheItem(accountInfo, item_info, 주문번호, 원주문번호, 매매구분, 주문가격, 주문수량, 잔량, STOP가격);
                                accountInfo.MicheItems.Add(new_item);
                            }
                        }
                    }
                }
            }
            // 해외주문미체결
            else if (e.nRealType == 0186)
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "계좌번호").Trim();
                var accountInfo = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (accountInfo != null)
                {
                    string 종목코드 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "종목").Trim();
                    var item_info = _axHDFCommAgent.GetItemInfo(종목코드);
                    if (item_info != null)
                    {
                        string 주문번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "주문번호").Trim();
                        string 원주문번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "최초원주문번호").Trim();
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "주문수량").Trim(), out int 주문수량);
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "체결수량").Trim(), out int 체결수량);
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "잔량").Trim(), out int 잔량);
                        var exist_item = accountInfo.MicheItems.FirstOrDefault(x => x.주문번호.Equals(주문번호));
                        OrderType 매매구분 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "매매구분").Trim().Equals("1") ? OrderType.매수 : OrderType.매도;
                        double 주문가격 = double.Parse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "주문가격").Trim());
                        double STOP가격 = double.Parse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "STOP 가격").Trim());
                        if (exist_item != null)
                        {
                            if (잔량 == 0)
                                accountInfo.MicheItems.Remove(exist_item);
                            else
                            {
                                exist_item.주문수량 = 주문수량;
                                exist_item.주문잔량 = 잔량;
                                exist_item.주문가격 = 주문가격;
                                exist_item.STOP가격 = STOP가격;
                            }
                        }
                        else
                        {
                            if (잔량 > 0)
                            {
                                var new_item = new MicheItem(accountInfo, item_info, 주문번호, 원주문번호, 매매구분, 주문가격, 주문수량, 잔량, STOP가격);
                                accountInfo.MicheItems.Add(new_item);
                            }
                        }
                    }
                }
            }
            // 국내 주문미결제 (매수와 매도 구분되서 같이 들어온다
            else if (e.nRealType == 0183)
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "계좌번호").Trim();
                var accountInfo = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (accountInfo != null)
                {
                    string 종목코드 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "종목").Trim();
                    var item_info = _axHDFCommAgent.GetItemInfo(종목코드);
                    if (item_info != null)
                    {
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "당일미결제수량").Trim(), out int 당일순미결제수량);
                        OrderType 매매구분 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "매매구분").Trim().Equals("1") ? OrderType.매수 : OrderType.매도;
                        var exist_jango = accountInfo.JangoItems.FirstOrDefault(x => x.종목정보 == item_info && x.매매구분 == 매매구분);
                        double 평균단가 = double.Parse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "평균단가").Trim());
                        if (exist_jango != null)
                        {
                            if (당일순미결제수량 == 0)
                                accountInfo.JangoItems.Remove(exist_jango);
                            else
                            {
                                exist_jango.보유수량 = 당일순미결제수량;
                                exist_jango.매매구분 = 매매구분;
                                exist_jango.평균단가 = 평균단가;
                            }
                        }
                        else
                        {
                            if (당일순미결제수량 > 0)
                            {
                                var new_jango = new JangoItem(accountInfo, item_info, 매매구분, 당일순미결제수량, 평균단가);
                                accountInfo.JangoItems.Add(new_jango);
                            }
                        }
                    }
                }
            }
            // 해외 주문순미결제
            else if (e.nRealType == 0190)
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "계좌번호").Trim();
                var accountInfo = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (accountInfo != null)
                {
                    string 종목코드 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "종목").Trim();
                    var item_info = _axHDFCommAgent.GetItemInfo(종목코드);
                    if (item_info != null)
                    {
                        int.TryParse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "당일순미결제수량").Trim(), out int 당일순미결제수량);
                        var exist_jango = accountInfo.JangoItems.FirstOrDefault(x => x.종목정보 == item_info);
                        OrderType 매매구분 = _axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "매매구분").Trim().Equals("1") ? OrderType.매수 : OrderType.매도;
                        double 평균단가 = double.Parse(_axHDFCommAgent.CommGetData(e.sJongmokCode, e.nRealType, "OutRec1", 0, "평균단가").Trim());
                        if (exist_jango != null)
                        {
                            if (당일순미결제수량 == 0)
                                accountInfo.JangoItems.Remove(exist_jango);
                            else
                            {
                                exist_jango.보유수량 = 당일순미결제수량;
                                exist_jango.매매구분 = 매매구분;
                                exist_jango.평균단가 = 평균단가;
                            }
                        }
                        else
                        {
                            if (당일순미결제수량 > 0)
                            {
                                var new_jango = new JangoItem(accountInfo, item_info, 매매구분, 당일순미결제수량, 평균단가);
                                accountInfo.JangoItems.Add(new_jango);
                            }
                        }
                    }
                }
            }
            // 국내선물체결실시간
            else if (e.nRealType == 0065)
            {
                var byteBufs = _krEncoder.GetBytes(recv_text);
                string 종목코드 = _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.종목코드], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.종목코드]).Trim();
                if (_axHDFCommAgent.GetItemInfo(종목코드) is StockItemInfo itemInfo)
                {
                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.시가], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.시가]).Trim()
                        , out double 시가);
                    시가 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.고가], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.고가]).Trim()
                        , out double 고가);
                    고가 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.저가], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.저가]).Trim()
                        , out double 저가);
                    저가 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.누적거래량], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.누적거래량]).Trim()
                        , out double 누적거래량);

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.체결시간], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.체결시간]).Trim()
                        , out double 체결시간);

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.체결량], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.체결량]).Trim()
                        , out double 체결량);

                    var 체결구분 =
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.체결량], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.체결량]).Trim().Equals("+")
                        ? ChegyolType.매수 : ChegyolType.매도;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.전일대비], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.전일대비]).Trim()
                        , out double 전일대비);
                    전일대비 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0065.PerIndex[(int)STRUCT_0065.Kind.현재가], STRUCT_0065.PerSize[(int)STRUCT_0065.Kind.현재가]).Trim()
                        , out double 현재가);
                    현재가 *= itemInfo.소수점비율;

                    itemInfo.전일가 = 현재가 + 전일대비;
                    itemInfo.시가 = 시가;
                    itemInfo.고가 = 고가;
                    itemInfo.저가 = 저가;
                    itemInfo.누적거래량 = 누적거래량;
                    itemInfo.체결시간 = 체결시간;
                    itemInfo.체결량 = 체결량;
                    itemInfo.체결구분 = 체결구분;

                    itemInfo.현재가 = 현재가;

                    // 잔고데이터 업데이트
                    foreach (var account in _axHDFCommAgent.AccountInfos)
                    {
                        if (account.계좌구분 == AccountInfo.KIND.국내)
                        {
                            var jango = account.JangoItems.FirstOrDefault(x => x.종목정보 == itemInfo);
                            if (jango != null)
                            {
                                double 가격차 = itemInfo.현재가 - jango.평균단가;
                                if (jango.매매구분 == OrderType.매도) 가격차 = -가격차;
                                jango.손익 = itemInfo.손익계산(jango.보유수량, 가격차);
                            }
                        }
                    }
                }
            }
            // 해외선물체결실시간
            else if (e.nRealType == 0082)
            {
                var byteBufs = _krEncoder.GetBytes(recv_text);
                string 종목코드 = _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.종목코드], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.종목코드]).Trim();
                if (_axHDFCommAgent.GetItemInfo(종목코드) is StockItemInfo itemInfo)
                {
                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.시가], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.시가]).Trim()
                        , out double 시가);
                    시가 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.고가], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.고가]).Trim()
                        , out double 고가);
                    고가 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.저가], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.저가]).Trim()
                        , out double 저가);
                    저가 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.누적거래량], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.누적거래량]).Trim()
                        , out double 누적거래량);

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.체결시간], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.체결시간]).Trim()
                        , out double 체결시간);

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.체결량], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.체결량]).Trim()
                        , out double 체결량);

                    var 체결구분 =
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.체결량], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.체결량]).Trim().Equals("+")
                        ? ChegyolType.매수 : ChegyolType.매도;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.전일대비], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.전일대비]).Trim()
                        , out double 전일대비);
                    전일대비 *= itemInfo.소수점비율;

                    double.TryParse(
                        _krEncoder.GetString(byteBufs, STRUCT_0082.PerIndex[(int)STRUCT_0082.Kind.체결가], STRUCT_0082.PerSize[(int)STRUCT_0082.Kind.체결가]).Trim()
                        , out double 현재가);
                    현재가 *= itemInfo.소수점비율;

                    itemInfo.전일가 = 현재가 + 전일대비;
                    itemInfo.시가 = 시가;
                    itemInfo.고가 = 고가;
                    itemInfo.저가 = 저가;
                    itemInfo.누적거래량 = 누적거래량;
                    itemInfo.체결시간 = 체결시간;
                    itemInfo.체결량 = 체결량;
                    itemInfo.체결구분 = 체결구분;

                    itemInfo.현재가 = 현재가;

                    // 잔고데이터 업데이트
                    foreach (var account in _axHDFCommAgent.AccountInfos)
                    {
                        if (account.계좌구분 == AccountInfo.KIND.해외)
                        {
                            var jango = account.JangoItems.FirstOrDefault(x => x.종목정보 == itemInfo);
                            if (jango != null)
                            {
                                double 가격차 = itemInfo.현재가 - jango.평균단가;
                                if (jango.매매구분 == OrderType.매도) 가격차 = -가격차;
                                jango.손익 = itemInfo.손익계산(jango.보유수량, 가격차);
                            }
                        }
                    }
                }
            }


            // 주문관련 데이터는 따로 탭에 표시
            if (trData.DefReqData != null && trData.DefReqData.ReqKind_Sub == REQKIND_SUB.주문)
            {
                OutputLog(LogKind.주문실시간, $"계좌번호={e.sJongmokCode}, TrCode={e.nRealType:d4}, {trData.TRName}");
                List<string> list = new List<string>();
                var byteBufs = _krEncoder.GetBytes(recv_text);
                int byteIndex = 0;
                for (int i = 0; i < trData.OutRec1Names.Count; i++)
                {
                    int size = trData.OutRec1Sizes[i];
                    string order_content = _krEncoder.GetString(byteBufs, byteIndex, size);
                    list.Add($"{e.sJongmokCode} : [{e.nRealType:d4}] : {trData.OutRec1Names[i]} = {order_content}");
                    byteIndex += size;
                }
                OutputLog(LogKind.주문실시간, list);
            }
        }
        else
        {
            string msg = $"sJongmokCode={e.sJongmokCode}, nRealType={e.nRealType}";
            OutputLog(LogKind.OnGetBroadData, msg);
        }
    }

    //private void _axHDFCommAgent_OnGetData(object sender, _DHDFCommAgentEvents_OnGetDataEvent e)
    //{
    //    string msg = $"nType={e.nType}, wParam={e.wParam}, lParam={e.lParam}";
    //    OutputLog(LogKind.OnGetData, msg);
    //}

    private void _axHDFCommAgent_OnDataRecv(object sender, _DHDFCommAgentEvents_OnDataRecvEvent e)
    {
        DateTime Now = DateTime.Now;
        int nRepeatCnt1 = _axHDFCommAgent.CommGetRepeatCnt(e.sTrCode, -1, "OutRec1");
        int nRepeatCnt2 = _axHDFCommAgent.CommGetRepeatCnt(e.sTrCode, -1, "OutRec2");
        string strNextKey = _axHDFCommAgent.CommGetNextKey(e.nRqId, string.Empty);
        string msg = $"sTrCode={e.sTrCode}, nRqId={e.nRqId}, nRepeatCnt1={nRepeatCnt1}, nRepeatCnt2={nRepeatCnt2}, strNextKey=\"{strNextKey}\"";
        OutputLog(LogKind.OnDataRecv, msg);

        TRData? trData = HDFTrManager.GetTRData(e.sTrCode);
        if (trData != null)
        {
            // 선행 처리부분
            // 마스터코드 접수
            if (trData.TRCode.Equals("v90001"))
            {
                int.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", 0, "파일크기"), out int nFileSize);
                string strFileNm = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", 0, "파일명");
                if (strFileNm.Length > 0)
                    strFileNm = strFileNm.Split('\0')[0];
                string strProcCd = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", 0, "응답코드");
                if (strProcCd.Equals("REOK"))
                {
                    string strBuff = _axHDFCommAgent.CommGetDataDirect(trData.TRCode, -1, 128 + 4 + 8, nFileSize, 0, "A");
                    string path = _apiFolderPath + "\\mst\\" + strFileNm;
                    try
                    {
                        File.WriteAllText(path, strBuff, _krEncoder);
                        OutputLog(LogKind.LOGS, $"마스터파일 수신 완료 : {nFileSize}바이트, {path}");
                    }
                    catch (Exception)
                    {
                        OutputLog(LogKind.LOGS, $"파일쓰기 오류: {path}");
                    }

                    if (strFileNm.Equals("futcode.cod"))
                    {
                        if (_kr_JMCodes.Count == 0)
                        {
                            AddTreeItemInfos_KR_Futcode();
                        }
                    }
                }
                else
                {
                    OutputLog(LogKind.LOGS, $"마스터파일 수신 실패 : {strFileNm}, {strProcCd}");
                }
                return;
            }

            // 계좌 잔고/미체결
            // 해외 미체결내역조회
            else if (trData.TRCode.Equals("g11004.AQ0401%"))
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", 0, "계좌번호").Trim();
                var 계좌 = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (계좌 != null)
                {
                    계좌.MicheItems.Clear();
                    for (int i = 0; i < nRepeatCnt1; i++)
                    {
                        string 종목코드 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "종목코드").Trim();
                        var item_ing = _axHDFCommAgent.GetItemInfo(종목코드);
                        if (item_ing != null)
                        {
                            var 주문번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "주문번호").Trim();
                            var 최초원주문번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "최초원주문번호").Trim();
                            OrderType 매매구분 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "매매구분").Equals("1") ? OrderType.매수 : OrderType.매도;
                            double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "주문가격"), out double 주문가격);
                            int.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "주문수량"), out int 주문수량);
                            int.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "체결수량"), out int 체결수량);
                            double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "STOP가격"), out double STOP가격);

                            var newItem = new MicheItem(계좌, item_ing, 주문번호, 최초원주문번호, 매매구분, 주문가격, 주문수량, 체결수량, STOP가격);
                            계좌.MicheItems.Add(newItem);
                        }
                    }
                    계좌.MicheInited = true;
                }
            }
            // 해외 미결제내역조회
            else if (trData.TRCode.Equals("g11004.AQ0403%"))
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", 0, "계좌번호").Trim();
                var 계좌 = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (계좌 != null)
                {
                    계좌.JangoItems.Clear();
                    for (int i = 0; i < nRepeatCnt1; i++)
                    {
                        string 종목코드 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "종목").Trim();
                        var item_ing = _axHDFCommAgent.GetItemInfo(종목코드);
                        if (item_ing != null)
                        {
                            OrderType 매매구분 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "매매구분").Equals("1") ? OrderType.매수 : OrderType.매도;
                            int.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "당일순 미결제수량"), out int 수량);
                            double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "평균단가(소수점반영)"), out double 평균단가);
                            double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "평가손익"), out double 평가손익);

                            var newItem = new JangoItem(계좌, item_ing, 매매구분, 수량, 평균단가)
                            {
                                손익 = 평가손익
                            };
                            계좌.JangoItems.Add(newItem);
                        }
                    }
                    계좌.JangoInited = true;
                }
            }
            // 국내 미체결내역조회
            else if (trData.TRCode.Equals("g11002.DQ0104&"))
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", 0, "계좌번호").Trim();
                var 계좌 = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (계좌 != null)
                {
                    계좌.MicheItems.Clear();
                    for (int i = 0; i < nRepeatCnt1; i++)
                    {
                        string 종목코드 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "종목코드").Trim();
                        var item_ing = _axHDFCommAgent.GetItemInfo(종목코드);
                        if (item_ing != null)
                        {
                            var 주문번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "주문번호").Trim();
                            var 최초원주문번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "최초원주문번호").Trim();
                            OrderType 매매구분 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "매매구분").Equals("1") ? OrderType.매수 : OrderType.매도;
                            double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "주문가격"), out double 주문가격);
                            int.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "주문수량"), out int 주문수량);
                            int.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "체결수량"), out int 체결수량);
                            //double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "STOP가격"), out double STOP가격);

                            var newItem = new MicheItem(계좌, item_ing, 주문번호, 최초원주문번호, 매매구분, 주문가격, 주문수량, 체결수량, 0);
                            계좌.MicheItems.Add(newItem);
                        }
                    }
                    계좌.MicheInited = true;
                }
            }
            // 국내 미결제내역조회
            else if (trData.TRCode.Equals("g11002.DQ1305&"))
            {
                string 계좌번호 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", 0, "계좌번호").Trim();
                var 계좌 = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌번호.Equals(계좌번호));
                if (계좌 != null)
                {
                    계좌.JangoItems.Clear();
                    for (int i = 0; i < nRepeatCnt1; i++)
                    {
                        string 종목코드 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "종목").Trim();
                        var item_ing = _axHDFCommAgent.GetItemInfo(종목코드);
                        if (item_ing != null)
                        {
                            OrderType 매매구분 = _axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "매매구분").Equals("1") ? OrderType.매수 : OrderType.매도;
                            int.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "당일미결제수량"), out int 수량);
                            double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "평균단가"), out double 평균단가);
                            double 평가손익 = 0;
                            if (평균단가 != 0)
                            {
                                double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "평가손익"), out 평가손익);
                            }
                            else
                            {
                                double.TryParse(_axHDFCommAgent.CommGetData(trData.TRCode, -1, "OutRec1", i, "장부단가"), out 평균단가);
                            }

                            var newItem = new JangoItem(계좌, item_ing, 매매구분, 수량, 평균단가)
                            {
                                손익 = 평가손익
                            };
                            계좌.JangoItems.Add(newItem);
                        }
                    }
                    계좌.JangoInited = true;
                }
            }
            // 차트요청
            // 국내 차트요청 결과
            else if (e.nRqId == _krFutureChartModel.NRqId)
            {
                StringBuilder stringBuilder = new StringBuilder();

                for (int i = 0; i < nRepeatCnt2; i++)
                {
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "날짜시간"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "시가"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "고가"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "저가"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "종가"));
                    stringBuilder.AppendLine(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "거래량"));
                }

                _krFutureChartModel.ReceivedTime = Now;
                _krFutureChartModel.ReceivedDataCount = nRepeatCnt2;
                _krFutureChartModel.ResultText = stringBuilder.ToString();
            }
            // 해외 차트요청 결과
            else if (e.nRqId == _hwFutureChartModel.NRqId)
            {
                StringBuilder stringBuilder = new StringBuilder();

                for (int i = 0; i < nRepeatCnt2; i++)
                {
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "국내일자"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "국내시간"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "시가"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "고가"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "저가"));
                    stringBuilder.Append(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "종가"));
                    stringBuilder.AppendLine(_axHDFCommAgent.CommGetData(e.sTrCode, -1, "OutRec2", i, "체결량"));
                }

                _hwFutureChartModel.ReceivedTime = Now;
                _hwFutureChartModel.ReceivedDataCount = nRepeatCnt2;
                _hwFutureChartModel.ResultText = stringBuilder.ToString();
            }

            List<string> list = [];
            // TR출력 데이터 파싱
            {
                int nOffest = 0;
                if (trData.OutputTotalSize > 0)
                {
                    int output_size = trData.OutputTotalSize;
                    string OutputData = _axHDFCommAgent.CommGetDataDirect(e.sTrCode, -1, nOffest, output_size, 0, "A");
                    var bytes = _krEncoder.GetBytes(OutputData);
                    if (bytes.Length == output_size)
                    {
                        int byteIndex = 0;
                        for (int i = 0; i < trData.OutputNames.Count; i++)
                        {
                            int per_size = trData.OutputSizes[i];
                            string per_data = _krEncoder.GetString(bytes, byteIndex, per_size);
                            list.Add(string.Format("{0}:Output : {1} = {2})"
                                , e.sTrCode
                                , trData.OutputNames[i]
                                , per_data));
                            byteIndex += per_size;
                        }
                    }
                    nOffest += trData.OutputTotalSize;
                }
                if (nRepeatCnt1 > 0)
                {
                    nOffest += trData.OutRec1RowCountDigit;
                    int nFrameSize = trData.OutRec1TotalSize;
                    // // Fid조회는 필드 마지막에 구분자 1자리가 있으므로 각 필드 만큼 더해준다.
                    if (trData.DefReqData != null && trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommFIDRqData)
                    {
                        nOffest -= trData.OutRec1RowCountDigit;
                        nFrameSize += trData.OutRec1Names.Count;
                    }
                    int size1 = nFrameSize * nRepeatCnt1;
                    string OutRec1Data = _axHDFCommAgent.CommGetDataDirect(e.sTrCode, -1, nOffest, size1, 0, "A");
                    var bytes1 = _krEncoder.GetBytes(OutRec1Data);
                    if (bytes1.Length == size1)
                    {
                        for (int i = 0; i < nRepeatCnt1; i++)
                        {
                            int recorIndex = i * nFrameSize;
                            string perData = _krEncoder.GetString(bytes1, recorIndex, nFrameSize);
                            list.Add(string.Format("{0}:OutRec1 : [{1:d4}] : {2})"
                                , e.sTrCode
                                , i
                                , perData));

                            // 계좌관련 조회는 세분으로 표시한다
                            if (trData.DefReqData != null && trData.DefReqData.NeedAccount)
                            {
                                int byteIndex = 0;
                                for (int k = 0; k < trData.OutRec1Names.Count; k++)
                                {
                                    int per_size = trData.OutRec1Sizes[k];
                                    string per_data = _krEncoder.GetString(bytes1, recorIndex + byteIndex, per_size);
                                    list.Add($"{trData.OutRec1Names[k]} = {per_data}");
                                    byteIndex += per_size;
                                }
                            }
                        }
                    }
                    else
                    {
                        list.Add($"OutRec1 size not equal : Req={size1}, Res={bytes1.Length}");
                        string fullData = _krEncoder.GetString(bytes1, 0, bytes1.Length);
                        list.Add(string.Format("{0}:OutRec1 : {1})"
                                , e.sTrCode
                                , fullData));
                    }
                    nOffest += size1;
                }
                if (nRepeatCnt2 > 0)
                {
                    nOffest += trData.OutRec2RowCountDigit;
                    int size2 = trData.OutRec2TotalSize * nRepeatCnt2;
                    string OutRec2Data = _axHDFCommAgent.CommGetDataDirect(e.sTrCode, -1, nOffest, size2, 0, "A");
                    var bytes2 = _krEncoder.GetBytes(OutRec2Data);
                    if (bytes2.Length == size2)
                    {
                        for (int i = 0; i < nRepeatCnt2; i++)
                        {
                            string perData = _krEncoder.GetString(bytes2, i * trData.OutRec2TotalSize, trData.OutRec2TotalSize);
                            list.Add(string.Format("{0}:OutRec2 : [{1:d4}] : {2})"
                                , e.sTrCode
                                , i
                                , perData));
                        }
                    }
                    else
                    {
                        list.Add($"OutRec2 size not equal : Req={size2}, Res={bytes2.Length}");
                    }
                }
                OutputLog(LogKind.OnDataRecv, list);
            }

            // 추가 계좌정보 수신부분
            if (trData.TRCode.Equals("g11004.AQ0101%"))
            {
                ParsingAccountInfo(_axHDFCommAgent.CommGetAccInfo());

                // 주문관련 실시간 체결 등록
                foreach (var account in _axHDFCommAgent.AccountInfos)
                {
                    int ret = _axHDFCommAgent.CommSetJumunChe(_axHDFCommAgent.LoginInfo.UserID, account.계좌번호);
                    if (ret < 0)
                        OutputLog(LogKind.LOGS, $"실시간 주문/체결 등록오류 ({account.계좌번호})");
                }
            }
        }
    }

    //private void _axHDFCommAgent_OnRealData(object sender, _DHDFCommAgentEvents_OnRealDataEvent e)
    //{
    //    string msg = $"nType={e.nType}, wParam={e.wParam}, lParam={e.lParam}";
    //    OutputLog(LogKind.OnRealData, msg);
    //}

}

