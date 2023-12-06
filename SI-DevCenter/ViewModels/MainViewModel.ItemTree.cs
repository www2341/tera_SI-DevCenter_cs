using SI_DevCenter.Helpers;
using SI_DevCenter.Models;
using SI_DevCenter.Models.StructModels;
using SI_DevCenter.Repositories;
using StockDevControl.Models;
using StockDevControl.StockModels;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    private IList<TRData>? _trDatas;
    private async void LoadTRListsAsync()
    {
        if (_trDatas != null) return;
        var TrDatas_Errors = await HDFTrManager.LoadAllTRListsAsync().ConfigureAwait(true);
        if (TrDatas_Errors == null) return;

        _trDatas = TrDatas_Errors.Item1;
        IList<string> Errors = TrDatas_Errors.Item2;
        foreach (var error in Errors)
        {
            OutputLog(LogKind.LOGS, error);
        }
        if (_trDatas != null)
        {

            var defReqs = HDFTrManager.PreDefineReqs;

            TabTreeDatas[0].OrgItems = new List<object>() { await HDFTrManager.CreateAllTrFiles(_trDatas).ConfigureAwait(true) };
            TabTreeDatas[1].OrgItems = new List<object>() { await HDFTrManager.CreateTrMainItem(defReqs).ConfigureAwait(true) };
            TabTreeDatas[2].OrgItems = new List<object>() { await HDFTrManager.CreateServiceMainItem(defReqs).ConfigureAwait(true) };
            TabTreeDatas[3].OrgItems = new List<object>() { await HDFTrManager.CreateInstanceFuncs(_axHDFCommAgent).ConfigureAwait(true) };

            OutputLog(LogKind.LOGS, $"Loaded: TR목록({_trDatas.Count})");
        }
    }

    // 해외선물 종목데이터 (JMCODE.cod)
    List<string[]> _hw_JMCodes = [];

    // 해외선물 품목데이터 (MRKT.cod)
    List<string[]> _hw_MRKTs = [];
    List<string> _hw_Fut상품군목록 = []; // 금리, 농산물, 에너지, 지수, 축산물, 통화


    // 국내선물 품목데이터 (futcode.cod)
    List<string[]> _kr_JMCodes = [];

    // 선물 종목 정보
    private async void AddTreeItemInfos_HW_JMCode()
    {
        //_axHDFCommAgent.KRFutureItemInfos
        var task = Task.Factory.StartNew(() =>
        {
            _hw_JMCodes.Clear();
            _hw_MRKTs.Clear();
            _hw_Fut상품군목록.Clear();

            _axHDFCommAgent.HWFutureItemInfos.Clear();
            _axHDFCommAgent.HWFutureKwanItemInfos.Clear();

            // 해외선물 종목 데이터 로딩
            IdTextItem 해외선물종목 = new(11, "해외선물종목") { IsExpanded = true };
            List<string> errors = new List<string>();
            try
            {
                string path = _apiFolderPath + "\\mst\\JMCODE.cod";
                var lines = File.ReadAllLines(path, _krEncoder);

                int UnitCount = STRUCT_JMCODE.PerSize.Length;
                int FrameSize = STRUCT_JMCODE.PerIndex[UnitCount - 1] + STRUCT_JMCODE.PerSize[UnitCount - 1];

                foreach (var line in lines)
                {
                    var byDatas = _krEncoder.GetBytes(line);
                    if (byDatas.Length == FrameSize)
                    {
                        var units = new string[UnitCount];
                        for (int i = 0; i < UnitCount; i++)
                        {
                            units[i] = _krEncoder.GetString(byDatas, STRUCT_JMCODE.PerIndex[i], STRUCT_JMCODE.PerSize[i]).Trim();
                        }
                        _hw_JMCodes.Add(units);
                        double.TryParse(units[(int)STRUCT_JMCODE.Kind.TickSize], out double 틱사이즈);
                        double.TryParse(units[(int)STRUCT_JMCODE.Kind.계약크기], out double 계약크기);
                        int.TryParse(units[(int)STRUCT_JMCODE.Kind.소수점정보], out int 소수점정보);
                        StockItemInfo item_info = new(units[(int)STRUCT_JMCODE.Kind.종목코드], units[(int)STRUCT_JMCODE.Kind.Full종목명한글], 틱사이즈, 계약크기, 소수점정보, units[(int)STRUCT_JMCODE.Kind.결제통화코드]);
                        _axHDFCommAgent.HWFutureItemInfos.Add(item_info);
                        if (units[(int)STRUCT_JMCODE.Kind.최근월물].Equals("1")) _axHDFCommAgent.HWFutureKwanItemInfos.Add(item_info);

                    }
                    else
                    {
                        errors.Add($"JMCODE.cod 파일 오류: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                _hw_JMCodes.Clear();
                _axHDFCommAgent.HWFutureItemInfos.Clear();
                _axHDFCommAgent.HWFutureKwanItemInfos.Clear();
                errors.Add(ex.Message);
                return (errors, 해외선물종목);
            }
            finally
            {
                _axHDFCommAgent.ReMakeCodeToItemnfoDictionary();
            }
            // 관심종목 소팅

            (_axHDFCommAgent.HWFutureKwanItemInfos as List<StockItemInfo>)?.Sort();

            // 해외선물 품목 데이터 로딩
            try
            {
                string path = _apiFolderPath + "\\mst\\MRKT.cod";
                var lines = File.ReadAllLines(path, _krEncoder);

                int UnitCount = STRUCT_MRKT.PerSize.Length;
                int FrameSize = STRUCT_MRKT.PerIndex[UnitCount - 1] + STRUCT_MRKT.PerSize[UnitCount - 1];

                foreach (var line in lines)
                {
                    var byDatas = _krEncoder.GetBytes(line);
                    if (byDatas.Length == FrameSize)
                    {
                        var units = new string[UnitCount];
                        for (int i = 0; i < UnitCount; i++)
                        {
                            units[i] = _krEncoder.GetString(byDatas, STRUCT_MRKT.PerIndex[i], STRUCT_MRKT.PerSize[i]).Trim();
                        }
                        _hw_MRKTs.Add(units);
                    }
                    else
                    {
                        errors.Add($"MRKT.cod 파일 오류: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                _hw_MRKTs.Clear();
                errors.Add(ex.Message);
                return (errors, 해외선물종목);
            }

            // 상품군 목록 등록
            foreach (var mrkt in _hw_MRKTs)
            {
                var 상품군 = mrkt[(int)STRUCT_MRKT.Kind.상품군];
                if (_hw_Fut상품군목록.FirstOrDefault(x => x.Equals(상품군)) == null)
                {
                    _hw_Fut상품군목록.Add(상품군);
                }
            }

            // 해외선물 품목 아이템 등록
            IdTextItem 전체종목 = new(1, $"전체종목({_hw_JMCodes.Count})");
            IdTextItem 최근월물 = new(1, "최근월물") { IsExpanded = true };
            해외선물종목.AddChild(전체종목);
            해외선물종목.AddChild(최근월물);

            var objType = nameof(STRUCT_JMCODE);
            int n최근Count = 0;
            foreach (var 상품군 in _hw_Fut상품군목록)
            {
                IdTextItem 전체 = new(8, $"{상품군}");
                IdTextItem 최근 = new(8, $"{상품군}");
                var match_mrkt_s = _hw_MRKTs.Where(x => x[(int)STRUCT_MRKT.Kind.상품군].Equals(상품군));
                foreach (var mrkt in match_mrkt_s)
                {
                    var filtered = _hw_JMCodes.Where(x => x[(int)STRUCT_JMCODE.Kind.품목코드].Equals(mrkt[(int)STRUCT_MRKT.Kind.시장구분코드]));
                    foreach (var code in filtered)
                    {
                        bool b최근월물 = code[(int)STRUCT_JMCODE.Kind.최근월물].Equals("1");
                        IdTextItem newItem = new(b최근월물 ? 9 : 6, code[(int)STRUCT_JMCODE.Kind.Full종목명한글])
                        {
                            Tag = objType,
                            Key = code[(int)STRUCT_JMCODE.Kind.종목코드]
                        };
                        newItem.AddChild(new(13, $"종목코드 : {code[(int)STRUCT_JMCODE.Kind.종목코드]}"));
                        newItem.AddChild(new(13, $"종목명 : {code[(int)STRUCT_JMCODE.Kind.Full종목명]}"));
                        newItem.AddChild(new(13, $"잔존일수 : {code[(int)STRUCT_JMCODE.Kind.잔존일수]}"));
                        newItem.AddChild(new(13, $"TickSize : {GetValidNumber(code[(int)STRUCT_JMCODE.Kind.TickSize])}"));
                        newItem.AddChild(new(13, $"TickValue : {GetValidNumber(code[(int)STRUCT_JMCODE.Kind.TickValue])}"));
                        전체.AddChild(newItem);
                        if (b최근월물)
                            최근.AddChild(newItem);
                    }
                }
                전체.Name = $"{상품군}({전체.Items.Count})";
                최근.Name = $"{상품군}({최근.Items.Count})";
                전체종목.AddChild(전체);
                최근월물.AddChild(최근);
                n최근Count += 최근.Items.Count;
            }
            최근월물.Name = $"최근월물({n최근Count})";
            해외선물종목.Name = $"해외선물종목({_hw_JMCodes.Count})";

            return (errors, 해외선물종목);

        });

        var (errors, 해외선물종목) = await task.ConfigureAwait(true);
        if (errors.Count > 0) OutputLog(LogKind.LOGS, errors);

        var 사용자기능_root = TabTreeDatas[4].OrgItems![0] as IdTextItem;
        사용자기능_root!.AddChild(해외선물종목);
        OutputLog(LogKind.LOGS, $"해외선물 마스터 정보 로딩 완료, 종목개수={_hw_JMCodes.Count}");

        if (TabTreeDatas[4] == SelectedTabTreeData)
        {
            SelectedTabTreeData = null;
            OnPropertyChanged(nameof(SelectedTabTreeData));
            SelectedTabTreeData = TabTreeDatas[4];
            OnPropertyChanged(nameof(SelectedTabTreeData));
        }
    }

    private async void AddTreeItemInfos_KR_Futcode()
    {
        var task = Task.Factory.StartNew(() =>
        {
            _kr_JMCodes.Clear();
            _axHDFCommAgent.KRFutureItemInfos.Clear();

            IdTextItem 국내선물종목 = new(11, "국내선물종목");
            // 파일로부터 데이터 로딩
            List<string> errors = new List<string>();
            try
            {
                string path = _apiFolderPath + "\\mst\\futcode.cod";
                var lines = File.ReadAllLines(path, _krEncoder);

                int UnitCount = STRUCT_FUTCODE.PerSize.Length;
                int FrameSize = STRUCT_FUTCODE.PerIndex[UnitCount - 1] + STRUCT_FUTCODE.PerSize[UnitCount - 1];

                foreach (var line in lines)
                {
                    var byDatas = _krEncoder.GetBytes(line);
                    if (byDatas.Length == FrameSize)
                    {
                        var units = new string[UnitCount];
                        for (int i = 0; i < UnitCount; i++)
                        {
                            units[i] = _krEncoder.GetString(byDatas, STRUCT_FUTCODE.PerIndex[i], STRUCT_FUTCODE.PerSize[i]).Trim();
                        }
                        _kr_JMCodes.Add(units);
                        _axHDFCommAgent.KRFutureItemInfos.Add(new(units[(int)STRUCT_FUTCODE.Kind.종목코드], units[(int)STRUCT_FUTCODE.Kind.한글종목명], 0.25, 250000, 2, "KRW"));
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                _kr_JMCodes.Clear();
                _axHDFCommAgent.KRFutureItemInfos.Clear();
                return (errors, 국내선물종목);
            }
            finally
            {
                _axHDFCommAgent.ReMakeCodeToItemnfoDictionary();
            }

            var objType = nameof(STRUCT_FUTCODE);
            foreach (var code in _kr_JMCodes)
            {
                IdTextItem newItem = new(9, code[(int)STRUCT_FUTCODE.Kind.한글종목명]) { Tag = objType, Key = code[(int)STRUCT_FUTCODE.Kind.종목코드] };
                newItem.AddChild(new(13, $"종목코드 : {code[(int)STRUCT_FUTCODE.Kind.종목코드].Trim()}"));
                newItem.AddChild(new(13, $"호가단위 : {GetValidNumber(code[(int)STRUCT_FUTCODE.Kind.호가단위].Trim())}"));
                newItem.AddChild(new(13, $"거래승수 : {GetValidNumber(code[(int)STRUCT_FUTCODE.Kind.거래승수].Trim())}"));
                국내선물종목.AddChild(newItem);
            }

            return (errors, 국내선물종목);
        });

        var (errors, 국내선물종목) = await task.ConfigureAwait(true);
        if (errors.Count > 0) OutputLog(LogKind.LOGS, errors);

        국내선물종목.Name = $"국내선물종목({국내선물종목.Items.Count})";
        var 사용자기능_root = TabTreeDatas[4].OrgItems![0] as IdTextItem;
        사용자기능_root!.AddChild(국내선물종목);
        OutputLog(LogKind.LOGS, $"국내선물 마스터 정보 로딩 완료, 종목개수={_kr_JMCodes.Count}");

        if (TabTreeDatas[4] == SelectedTabTreeData)
        {
            SelectedTabTreeData = null;
            OnPropertyChanged(nameof(SelectedTabTreeData));
            SelectedTabTreeData = TabTreeDatas[4];
            OnPropertyChanged(nameof(SelectedTabTreeData));
        }
    }

    private static string GetValidNumber(string text)
    {
        string result = text.Trim('0');
        if (result.Length > 0)
        {
            if (result[0] == '.') result = result.Insert(0, "0");
            if (result[result.Length - 1] == '.') result += "0";
        }
        return result;
    }

    public List<TabTreeData> TabTreeDatas { get; }

    public TabTreeData? SelectedTabTreeData { get; set; }

    private IdTextItem? _selectedTreeItem;
    public IdTextItem? SelectedTreeItem
    {
        get => _selectedTreeItem;
        set
        {
            if (_selectedTreeItem == value) return;
            _selectedTreeItem = value;
            TreeView_SelectedItemChanged(_selectedTreeItem);
        }
    }

    private void SetResultWithFilePath(string filepath)
    {
        ResultPath = filepath;
        try
        {
            byte[] fileData = File.ReadAllBytes(filepath);
            string ansiText = _krEncoder.GetString(fileData, 0, fileData.Length);
            SetResultText(ansiText);

            TRData new_trData = new(filepath);
            HDFTrManager.ParsingTRData(ref new_trData, ansiText);

            SetPropertyItems(new_trData);

            SetEquitData(new_trData);
        }
        catch (Exception ex)
        {
            SetResultText($"Error: {filepath} : {ex.Message}");
        }
    }

    object? _equipTarget = null;
    private void SetEquitData(TRData trData)
    {
        _equipTarget = trData;
        // Equip Setting
        StringBuilder stringBuilder = new StringBuilder();
        if (trData.DefReqData != null)
        {
            if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommJumunSvr)
            {
                string sDummy = string.Empty;
                var altBytes = GetPropertyInputsBuf(PropertyData.PropertyItems, false, ref sDummy);

                int Length = altBytes.Length;
                // 프로퍼티 마지막속성이 '사용자정의필드' 인 경우 빈 문자이면 삭제
                if (PropertyData.PropertyItems.Count > 1
                    && PropertyData.PropertyItems[PropertyData.PropertyItems.Count - 1].Name.Equals("사용자정의필드")
                    && PropertyData.PropertyItems[PropertyData.PropertyItems.Count - 1].Value.Trim().Length == 0)
                    Length -= PropertyData.PropertyItems[PropertyData.PropertyItems.Count - 1].N;
                string sInputData = _krEncoder.GetString(altBytes, 0, Length);

                stringBuilder.AppendLine($"int result = CommJumunSvr(\"{trData.TRCode}\", \"{sInputData}\");");
            }
            else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommRqData)
            {
                string sDummy = string.Empty;
                var altBytes = GetPropertyInputsBuf(PropertyData.PropertyItems, false, ref sDummy);
                string sInputData = _krEncoder.GetString(altBytes);

                stringBuilder.AppendLine($"int result = CommRqData(\"{trData.TRCode}\", \"{sInputData}\", {altBytes.Length}, \"\");");
            }
            else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommFIDRqData)
            {
                string sDummy = string.Empty;
                var altBytes = GetPropertyInputsBuf(PropertyData.PropertyItems, false, ref sDummy);
                string sInputData = _krEncoder.GetString(altBytes);
                string sReqFidInput = GetFidNames(trData.OutRec1Names.Count);

                stringBuilder.AppendLine($"int result = CommFIDRqData(\"{trData.TRCode}\", \"{sInputData}\", \"{sReqFidInput}\", {altBytes.Length}, \"\");");
            }
            else if (trData.DefReqData.ReqKind_Func == REQKIND_FUNC.CommSetBroad)
            {
                int.TryParse(trData.TRCode, out int nRealType);

                // 종목코드는 Property값으로 세팅
                string sJongmokCode = string.Empty;
                if (PropertyData.PropertyItems.Count > 0)
                    sJongmokCode = PropertyData.PropertyItems[0].Value;
                stringBuilder.AppendLine($"int result = CommSetBroad(\"{sJongmokCode}\", {nRealType});");
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
                stringBuilder.AppendLine($"int result = CommSetJumunChe(\"{(IsLoggined ? _axHDFCommAgent.LoginInfo.UserID : "아이디")}\", \"{account?.계좌번호 ?? (IsLoggined ? "계좌없음" : "계좌번호")}\");");
            }
        }
        SetEquipText(stringBuilder.ToString());
    }

    private void TreeView_SelectedItemChanged(IdTextItem? selectedItem)
    {
        if (SelectedTabTreeData == null) return;
        if (selectedItem is null) return;

        MethodInfo? old_propertyMethod = _propertyTarget as MethodInfo;

        _propertyTarget = null;
        _equipTarget = null;
        UserContent = null;

        if (selectedItem.Tag is TRData trData)
        {
            SetResultWithFilePath(trData.FilePath);
        }
        else if (SelectedTabTreeData.Name.Equals("함수목록") && selectedItem.Tag is string funcName) // 함수목록 클릭
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            Type intanceType = _axHDFCommAgent.GetType();
            var methodInfo = intanceType.GetMethod(funcName);
            if (methodInfo != null)
            {
                string[] detail_rettypes = methodInfo.ReturnType.ToString().Split('.');
                stringBuilder.Append(detail_rettypes[detail_rettypes.Length - 1]);
                stringBuilder.Append(' ');
                stringBuilder.Append(funcName);
                stringBuilder.Append('(');

                //
                var parameters = methodInfo.GetParameters();
                int param_index = 0;
                foreach (var param in parameters)
                {
                    if (param_index > 0)
                        stringBuilder.Append(", ");
                    string[] detailtypes = param.ParameterType.ToString().Split('.');
                    stringBuilder.Append(detailtypes[detailtypes.Length - 1]);
                    stringBuilder.Append(" ");
                    stringBuilder.Append(param.Name);
                    param_index++;
                }
                //
                stringBuilder.Append(");");
                stringBuilder.AppendLine();

                SetPropertyItems(methodInfo);
            }

            ResultPath = string.Empty;
            if (old_propertyMethod != null)
                AddResultText(stringBuilder.ToString());
            else
                SetResultText(stringBuilder.ToString());
        }
        else if (SelectedTabTreeData.Name.Equals("사용자기능") && selectedItem.Tag is string userfunc)
        {
            if (!ProcUserTool(userfunc))
            {
                StringBuilder stringBuilder = new StringBuilder();
                if (userfunc.Equals("API정보"))
                {
                    string progID = "HDFCOMMAGENT.HDFCommAgentCtrl.1";
                    var classID = OcxPathHelper.GetClassIDFromProgID(progID);
                    var fileName = OcxPathHelper.GetOcxPathFromClassID(classID);
                    FileVersionInfo FileVersionInfo = FileVersionInfo.GetVersionInfo(fileName);
                    stringBuilder.AppendLine($"ProgID: \"{progID}\"");
                    stringBuilder.AppendLine($"CLSID: \"{classID}\"");
                    if (Environment.Is64BitProcess)
                    {
                        stringBuilder.AppendLine($"파일 경로(32비트): \"{OcxPathHelper.GetOcxPathFromWOW6432NodeCLSID(classID)}\"");
                        stringBuilder.AppendLine($"파일 경로(64비트): \"{fileName}\"");
                    }
                    else
                        stringBuilder.AppendLine($"파일 경로: \"{fileName}\"");
                    stringBuilder.AppendLine($"파일 설명: {FileVersionInfo.FileDescription}");
                    stringBuilder.AppendLine($"파일 버전: {FileVersionInfo.FileVersion}");
                }
                else if (userfunc.Equals("계좌정보"))
                {

                    for (int i = 0; i < _axHDFCommAgent.AccountInfos.Count; i++)
                    {
                        var unit = _axHDFCommAgent.AccountInfos[i];
                        stringBuilder.AppendLine($"계좌{i + 1}:");
                        stringBuilder.AppendLine($"\t계좌명: {unit.계좌명}");
                        stringBuilder.AppendLine($"\t계좌번호: {unit.계좌번호}");
                        stringBuilder.AppendLine($"\t계좌구분: {unit.계좌구분}({(int)unit.계좌구분})");
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.AppendLine("*** 계좌구분 '1': 해외, '2': FX, '9':국내");

                }
                else if (userfunc.Equals(nameof(STRUCT_JMCODE)))
                {
                    var find = _hw_JMCodes.FirstOrDefault(x => x[(int)STRUCT_JMCODE.Kind.종목코드].Equals(selectedItem.Key));

                    if (find != null)
                    {
                        var enumNames = Enum.GetNames(typeof(STRUCT_JMCODE.Kind));
                        int nIndex = 0;
                        foreach (var enumName in enumNames)
                        {
                            stringBuilder.AppendLine($"{enumName}: {find[nIndex]}");
                            nIndex++;
                        }
                    }
                }
                else if (userfunc.Equals(nameof(STRUCT_FUTCODE)))
                {
                    var find = _kr_JMCodes.FirstOrDefault(x => x[(int)STRUCT_FUTCODE.Kind.종목코드].Equals(selectedItem.Key));

                    if (find != null)
                    {
                        var enumNames = Enum.GetNames(typeof(STRUCT_FUTCODE.Kind));
                        int nIndex = 0;
                        foreach (var enumName in enumNames)
                        {
                            stringBuilder.AppendLine($"{enumName}: {find[nIndex]}");
                            nIndex++;
                        }
                    }
                }

                SetResultText(stringBuilder.ToString());
            }
        }

        ResultSaveCommand.NotifyCanExecuteChanged();
        QueryCommand.NotifyCanExecuteChanged();
    }
}

