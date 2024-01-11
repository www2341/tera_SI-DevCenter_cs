using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SI_DevCenter.Models;
using StockDevControl.Models;
using StockDevControl.StockModels;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    [RelayCommand(CanExecute = nameof(CanQuery))]
    void Query()
    {
        // 조회버튼
        if (_propertyTarget == null) return;

        var PropertyItems = PropertyData.PropertyItems;
        if (_propertyTarget is TRData trData)
        {
            string trCode = trData.TRCode;
            // 파라메터 검사
            string errorMsg = string.Empty;
            var alt_bytes = GetPropertyInputsBuf(PropertyItems, true, ref errorMsg);
            if (errorMsg.Length > 0)
            {
                OutputLog(LogKind.LOGS, $"{trCode} : {errorMsg}");
                return;
            }

            // 일단 데이터 보관
            foreach (var prop in PropertyItems)
            {
                _appRegistry.SetValue(trCode, prop.Name, prop.Value);
            }

            // 최근 조회 목록에 등록
            OutputLog(LogKind.최근조회TR, $"{trCode} : {trData.TRName}");

            // 실행
            if (_loginState != LoginState.LOGINED)
            {
                OutputLog(LogKind.LOGS, $"TR요청: {trCode} : 로그인 후 요청해 주세요");
                return;
            }

            if (trData.DefReqData == null)
            {
                OutputLog(LogKind.LOGS, $"정의된 함수로직이 없습니다. ({trCode})");
                return;
            }

            if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommJumunSvr)
            {
                int Length = alt_bytes.Length;
                // 프로퍼티 마지막속성이 '사용자정의필드' 인 경우 빈 문자이면 삭제
                if (PropertyData.PropertyItems.Count > 1
                    && PropertyData.PropertyItems[PropertyData.PropertyItems.Count - 1].Name.Equals("사용자정의필드")
                    && PropertyData.PropertyItems[PropertyData.PropertyItems.Count - 1].Value.Trim().Length == 0)
                    Length -= PropertyData.PropertyItems[PropertyData.PropertyItems.Count - 1].N;
                string sInputData = _krEncoder.GetString(alt_bytes, 0, Length);

                Stopwatch stopwatch = Stopwatch.StartNew();
                int result = _axHDFCommAgent.CommRqData(trCode, sInputData, Length, string.Empty);
                stopwatch.Stop();
                var Now = DateTime.Now;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS");
                stringBuilder.Append($", Return={result}");
                stringBuilder.AppendLine();
                EquipText += stringBuilder.ToString();
            }
            else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommRqData)
            {
                string sInputData = _krEncoder.GetString(alt_bytes);
                Stopwatch stopwatch = Stopwatch.StartNew();
                int result = _axHDFCommAgent.CommRqData(trCode, sInputData, alt_bytes.Length, string.Empty);
                stopwatch.Stop();
                var Now = DateTime.Now;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS");
                stringBuilder.Append($", result={result}");
                stringBuilder.AppendLine();
                EquipText += stringBuilder.ToString();
            }
            else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommFIDRqData)
            {
                string sInputData = _krEncoder.GetString(alt_bytes);
                string sReqFidInput = GetFidNames(trData.OutRec1Names.Count);

                Stopwatch stopwatch = Stopwatch.StartNew();
                int result = _axHDFCommAgent.CommFIDRqData(trCode, sInputData, sReqFidInput, alt_bytes.Length, string.Empty);
                stopwatch.Stop();
                var Now = DateTime.Now;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS");
                stringBuilder.Append($", result={result}");
                stringBuilder.AppendLine();
                EquipText += stringBuilder.ToString();
            }
            else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommSetBroad)
            {
                int.TryParse(trData.TRCode, out int nRealType);

                // 종목코드는 Property값으로 세팅
                string sJongmokCode = string.Empty;
                if (PropertyData.PropertyItems.Count > 0)
                    sJongmokCode = PropertyData.PropertyItems[0].Value;

                Stopwatch stopwatch = Stopwatch.StartNew();
                int result = _axHDFCommAgent.CommSetBroad(sJongmokCode, nRealType);
                stopwatch.Stop();
                var Now = DateTime.Now;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS");
                stringBuilder.Append($", result={result}");
                stringBuilder.AppendLine();
                EquipText += stringBuilder.ToString();
            }
            else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommSetJumunChe)
            {
                int.TryParse(trData.TRCode, out int nRealType);

                // 계좌구분 '1': 해외, '2': FX, '9':국내
                AccountInfo.KIND comp = trData.DefReqData.ReqKind_Main switch
                {
                    REQKIND_MAIN.국내 => AccountInfo.KIND.국내,
                    REQKIND_MAIN.해외 => AccountInfo.KIND.해외,
                    REQKIND_MAIN.FX => AccountInfo.KIND.FX,
                    _ => AccountInfo.KIND.UNKNOWN,
                };


                var account = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌구분 == comp);
                bool IsLoggined = _loginState == LoginState.LOGINED;

                string sUserID = IsLoggined ? _axHDFCommAgent.LoginInfo.UserID : "아이디";
                string sAccNo = account?.계좌번호 ?? (IsLoggined ? "계좌없음" : "계좌번호");

                Stopwatch stopwatch = Stopwatch.StartNew();
                int result = _axHDFCommAgent.CommSetJumunChe(sUserID, sAccNo);
                stopwatch.Stop();
                var Now = DateTime.Now;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS");
                stringBuilder.Append($", result={result}");
                stringBuilder.AppendLine();
                EquipText += stringBuilder.ToString();
            }
            else
            {
                OutputLog(LogKind.LOGS, $"요청 함수로직이 없습니다. ({trData.DefReqData.ReqKind_Func})");
            }
        }
        else if (_propertyTarget is MethodInfo methodInfo)
        {
            var inner_parameters = methodInfo.GetParameters();
            if (inner_parameters.Length != PropertyItems.Count)
            {
                OutputLog(LogKind.LOGS, "속성창 데이터 오류");
                return;
            }

            // 필수 검사
            if (methodInfo.Name.Equals("CommInit"))
            {
                if (_loginState != LoginState.CREATED)
                {
                    OutputLog(LogKind.LOGS, "CommInit 호출불가");
                    return;
                }
                PropertyItems[0].Value = "1"; // 무조건 1
            }
            else if (methodInfo.Name.Equals("CommTerminate"))
            {
                PropertyItems[0].Value = "1"; // 무조건 1
            }
            else if (methodInfo.Name.Equals("CommLogin"))
            {
                if (_loginState != LoginState.CONNECTED)
                {
                    OutputLog(LogKind.LOGS, "CommLogin 호출불가");
                    return;
                }
                _axHDFCommAgent.LoginInfo.UserID = PropertyItems[0].Value;
            }
            else if (methodInfo.Name.Equals("CommLogout") && _loginState != LoginState.LOGINED)
            {
                OutputLog(LogKind.LOGS, "CommLogout 호출불가");
                return;
            }

            // 일단 데이터 보관
            foreach (var prop in PropertyItems)
            {
                _appRegistry.SetValue(methodInfo.Name, prop.Name, prop.Value);
            }
            // 실행

            StringBuilder callStringBuilder = new StringBuilder();
            callStringBuilder.Append($"{methodInfo.Name}(");
            object?[] call_parmas = new object?[inner_parameters.Length];
            for (int i = 0; i < inner_parameters.Length; i++)
            {
                if (i != 0)
                    callStringBuilder.Append(", ");

                if (PropertyItems[i].type == StockDevControl.Models.PropertyItem.VALUE_TYPE.VALUE_LONG)
                {
                    int.TryParse(PropertyItems[i].Value, out int nValue);
                    call_parmas[i] = nValue;
                    callStringBuilder.Append(nValue);
                }
                else
                {
                    call_parmas[i] = Convert.ChangeType(PropertyItems[i].Value, inner_parameters[i].ParameterType);
                    callStringBuilder.Append($"\"{PropertyItems[i].Value}\"");
                }

            }
            callStringBuilder.Append(")");

            var Now = DateTime.Now;
            Stopwatch stopwatch = Stopwatch.StartNew();
            object? result = methodInfo.Invoke(_axHDFCommAgent, call_parmas);
            stopwatch.Stop();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"[{Now.ToString("HH:mm:ss.fff")}] : {stopwatch.Elapsed.TotalMilliseconds}mS : ");
            stringBuilder.Append(callStringBuilder);
            if (result != null)
            {
                stringBuilder.Append($", result={result}");
            }
            stringBuilder.AppendLine();
            AddResultText(stringBuilder.ToString());

            if (methodInfo.Name.Equals("CommInit"))
            {
                int ret = (int)result!;
                if (ret == 0)
                {
                    SetStatusText("통신관리자 실행 성공");
                    _loginState = LoginState.CONNECTED;
                }
                else
                    SetStatusText($"통신관리자 실행 오류: {ret}");

                OutputLog(LogKind.LOGS, StatusText);
            }
            else if (methodInfo.Name.Equals("CommTerminate"))
            {
                _loginState = LoginState.CREATED;
                SetStatusText("통신관리자 종료");
                OutputLog(LogKind.LOGS, StatusText);
            }
            else if (methodInfo.Name.Equals("CommLogin"))
            {
                int ret = (int)result!;
                if (ret > 0)
                {
                    SetStatusText("로그인 성공");
                    _loginState = LoginState.LOGINED;
                }
                else
                {
                    SetStatusText($"로그인 실패: {ret}");
                }
                OutputLog(LogKind.LOGS, StatusText);
            }
            else if (methodInfo.Name.Equals("CommLogout"))
            {
                int ret = (int)result!;
                if (ret == 0)
                {
                    SetStatusText("로그아웃 성공");
                    _loginState = LoginState.CONNECTED;
                }
                else
                {
                    SetStatusText($"로그아웃 실패: {ret}");
                }
                OutputLog(LogKind.LOGS, StatusText);
            }
        }

    }
    bool CanQuery() => _propertyTarget != null;

    object? _propertyTarget = null;

    [ObservableProperty]
    PropertyData _propertyData = new PropertyData() { HeaderText = "요청설정" };

    private void SetPropertyItems(TRData trData)
    {
        _propertyTarget = trData;

        PropertyData.PropertyItems.Clear();
        PropertyData.HeaderText = $"{trData.TRCode} : {trData.TRName}";

        if (trData.DefReqData == null) return;
        if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommSetJumunChe)
        {
        }
        else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommSetBroad)
        {
            string value = _appRegistry.GetValue(trData.TRCode, "sJongmokCode", string.Empty);
            PropertyData.PropertyItems.Add(new(0, "sJongmokCode", value, "실시간값을 받고자 하는 종목명"));
        }
        else
        {
            foreach (var inputdata in trData.InputDatas)
            {
                int digits = 0;
                string key = inputdata.Item1;
                string contents = inputdata.Item2; // 51,  1, 0, A;1매수, 2매도
                string desc = string.Empty;

                var val_desc = contents.Split(';');
                if (val_desc.Length >= 2)
                {
                    desc = val_desc[1].Trim();
                }
                var vals = contents.Split(',');
                if (vals.Length >= 2)
                {
                    digits = int.Parse(vals[1]);
                }

                string value = _appRegistry.GetValue(trData.TRCode, key, string.Empty);

                bool bReadOnly = false;
                if (key.Equals("구분값"))
                {
                    value = "0";
                    bReadOnly = true;
                }
                else if (key.Equals("처리구분"))
                {
                    bReadOnly = true;
                }
                else if (key.Equals("딜러번호"))
                {
                    value = "001";
                    bReadOnly = true;
                }
                else if (key.Equals("처리지점"))
                {
                    value = "001";
                    bReadOnly = true;
                }
                else if (key.Equals("그룹명"))
                {
                    value = string.Empty;
                    bReadOnly = true;
                }
                else if (_loginState == LoginState.LOGINED)
                {
                    if (key.Equals("로그인아이디"))
                    {
                        value = _axHDFCommAgent.LoginInfo.UserID;
                        bReadOnly = true;
                    }
                    else if (key.Equals("계좌번호"))
                    {
                        var DefReq = trData.DefReqData;
                        if (DefReq != null)
                        {
                            AccountInfo.KIND comp = trData.DefReqData.ReqKind_Main switch
                            {
                                REQKIND_MAIN.국내 => AccountInfo.KIND.국내,
                                REQKIND_MAIN.해외 => AccountInfo.KIND.해외,
                                REQKIND_MAIN.FX => AccountInfo.KIND.FX,
                                _ => AccountInfo.KIND.UNKNOWN,
                            };

                            var account = _axHDFCommAgent.AccountInfos.FirstOrDefault(x => x.계좌구분 == comp);
                            if (account != null)
                            {
                                value = account.계좌번호;
                            }
                        }
                    }
                    else if (key.Equals("비밀번호"))
                    {
                        if (_serverType == SERVER_TYPE.Simulation)
                        {
                            value = "1234";
                            bReadOnly = true;
                        }
                        else if (_serverType == SERVER_TYPE.Real)
                        {
                            if (value.Equals("1234")) value = string.Empty;
                        }
                    }
                }
                PropertyData.PropertyItems.Add(new(digits, key, value, desc, PropertyItem.VALUE_TYPE.VALUE_STRING, bReadOnly));
            }
        }
    }

    private void SetPropertyItems(MethodInfo methodInfo)
    {

        _propertyTarget = methodInfo;

        PropertyData.PropertyItems.Clear();
        PropertyData.HeaderText = $"함수호출 : {methodInfo.Name}";

        var parameters = methodInfo.GetParameters();
        int param_index = 0;
        foreach (var param in parameters)
        {
            string value = _appRegistry.GetValue(methodInfo.Name, param.Name!, string.Empty);
            bool IsString = param.ParameterType == typeof(string);
            param_index++;
            PropertyData.PropertyItems.Add(new(param_index, param.Name!, value, param.ParameterType.ToString(), IsString ? StockDevControl.Models.PropertyItem.VALUE_TYPE.VALUE_STRING : StockDevControl.Models.PropertyItem.VALUE_TYPE.VALUE_LONG));
        }
    }

    [RelayCommand]
    void PropertyCellEdited(int nRowIndex)
    {
        if (_equipTarget is TRData trData)
        {
            SetEquitData(trData);
        }
    }
}

