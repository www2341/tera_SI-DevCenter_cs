using CommunityToolkit.Mvvm.Input;
using SI_DevCenter.Models;
using SI_DevCenter.Views;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WritePrivateProfileStringW(string lpAppName, string lpKeyName, string lpString, string lpFileName);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetPrivateProfileStringW(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
    private static string GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, string lpFileName)
    {
        StringBuilder lpReturnedString = new StringBuilder(256);
        int ret = GetPrivateProfileStringW(lpAppName, lpKeyName, lpDefault, lpReturnedString, 256, lpFileName);
        return lpReturnedString.ToString();
    }

    enum SERVER_TYPE
    {
        UnKwnown,
        Simulation,
        Real
    }
    private SERVER_TYPE _requred_serverType = SERVER_TYPE.UnKwnown;
    private SERVER_TYPE _serverType = SERVER_TYPE.UnKwnown;
    private void UpdateServerType()
    {
        if (_axHDFCommAgent.AccountInfos.Count == 0) return;
        // 메인뷰 타이틀 변경
        // 계좌명으로 모의서버인가? 실서버인가? 확정
        if (_axHDFCommAgent.AccountInfos[0].계좌명.IndexOf("모의_") == 0) _serverType = SERVER_TYPE.Simulation;
        else _serverType = SERVER_TYPE.Real;

        Title = _baseTitle
            + (_serverType == SERVER_TYPE.Simulation ? "(모의서버)" : _serverType == SERVER_TYPE.Real ? "(실서버)" : string.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanMenuRealLogin))]
    private void MenuRealLogin()
    {
        MenuLogin(isRealServer: true);
    }

    [RelayCommand(CanExecute = nameof(CanMenuSimulLogin))]
    private void MenuSimulationLogin()
    {
        MenuLogin(isRealServer: false);
    }
    private void MenuLogin(bool isRealServer)
    {
        _requred_serverType = isRealServer ? SERVER_TYPE.Real : SERVER_TYPE.Simulation;

        if (_loginState == LoginState.CREATED)
        {
            // Commsu.ini 파일 검토
            string comms_path = _apiFolderPath + "\\system\\Commsu.ini";

            string oldValue = GetPrivateProfileString("STARTER", "Simulation", string.Empty, comms_path);
            if (oldValue.Length != 1 || !(oldValue[0] == '0' || oldValue[0] == '1'))
            {
                SetStatusText("시스템 파일 데이터가 정확치 않습니다.");
                return;
            }
            bool oldType_isRealServer = oldValue[0] == '0';

            if (isRealServer ^ oldType_isRealServer)
            {
                if (!WritePrivateProfileStringW("STARTER", "Simulation", isRealServer ? "0" : "1", comms_path))
                {
                    SetStatusText("시스템 파일을 변경할수 없습니다.");
                    return;
                }
            }

            _axHDFCommAgent.CommSetOCXPath(_apiFolderPath);
            int ret = _axHDFCommAgent.CommInit(1);

            if (ret == 0)
            {
                SetStatusText("통신관리자 실행 성공");
                _loginState = LoginState.CONNECTED;
            }
            else
                SetStatusText($"통신관리자 실행 오류: {ret}");
            OutputLog(LogKind.LOGS, StatusText);
        }

        if (_loginState == LoginState.CONNECTED)
        {
            // 통신관리자 연결 되었다면, 사용자 아이디, 페스워드 로그인
            // 사용자 아이디와 비번만 요구
            // 공인인증서는 요구하면 안됨, 사용자가 인증 완성토록..
            string key_userId = isRealServer ? "Real_UserID" : "UserID";
            string key_password = isRealServer ? "Real_Password" : "Password";
            string UserID = _appRegistry.GetValue("InitData", key_userId, string.Empty);
            string Password = _appRegistry.GetValue("InitData", key_password, string.Empty);

            var keySetting = new KeySettings(UserID, Password);
            if (_requred_serverType == SERVER_TYPE.Real)
                keySetting.Background = Brushes.PaleVioletRed;
            if (keySetting.ShowDialog() == true)
            {
                UserID = keySetting.UserID.Text;
                Password = keySetting.Password.Password;

                _appRegistry.SetValue("InitData", key_userId, UserID);
                _appRegistry.SetValue("InitData", key_password, Password);

                _axHDFCommAgent.LoginInfo.UserID = UserID;
                int ret = _axHDFCommAgent.CommLogin(UserID, Password, string.Empty);
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

                if (_loginState == LoginState.LOGINED)
                {
                    OutputLog(LogKind.LOGS, $"계좌정보 수신완료: 계좌개수={_axHDFCommAgent.AccountInfos.Count}");

                    // 국내선물종목이 수신되지 않았다면 불러오기
                    if (_kr_JMCodes.Count == 0)
                    {
                        OutputLog(LogKind.LOGS, "국내선물 마스터 정보 요청.");
                        _axHDFCommAgent.CommRqData("v90001", "futcode.cod", "futcode.cod".Length, string.Empty);
                    }
                    else
                    {
                        OutputLog(LogKind.LOGS, $"해외선물 종목 개수={_hw_JMCodes.Count}");
                    }

                    // 해외종목이 수신되지 않았다면 불러오기
                    if (_hw_JMCodes.Count == 0)
                    {
                        OutputLog(LogKind.LOGS, "해외선물 마스터 정보 요청.");
                        _axHDFCommAgent.CommReqMakeCod("futures", 0);
                    }
                    else
                    {
                        OutputLog(LogKind.LOGS, $"해외선물 종목 개수={_hw_JMCodes.Count}");
                    }
                }
            }
            else
            {
                OutputLog(LogKind.LOGS, "로그인 취소");
                SetStatusText("로그인 취소");
            }
        }

        MenuSimulationLoginCommand.NotifyCanExecuteChanged();
        MenuRealLoginCommand.NotifyCanExecuteChanged();
        MenuLogoutCommand.NotifyCanExecuteChanged();
    }

    private bool CanMenuRealLogin() => _loginState != LoginState.NONE && _loginState != LoginState.LOGINED && _requred_serverType != SERVER_TYPE.Simulation;
    private bool CanMenuSimulLogin() => _loginState != LoginState.NONE && _loginState != LoginState.LOGINED && _requred_serverType != SERVER_TYPE.Real;

    [RelayCommand(CanExecute = nameof(CanMenuLogout))]
    private void MenuLogout()
    {
        if (_loginState == LoginState.LOGINED)
        {
            _axHDFCommAgent.CommLogout(_axHDFCommAgent.LoginInfo.UserID);
            OutputLog(LogKind.LOGS, "로그인 해제");
        }
        _axHDFCommAgent.CommTerminate(1);
        _loginState = LoginState.CREATED;
        _requred_serverType = SERVER_TYPE.UnKwnown;

        SetStatusText("통신관리자 종료");
        OutputLog(LogKind.LOGS, StatusText);

        MenuSimulationLoginCommand.NotifyCanExecuteChanged();
        MenuRealLoginCommand.NotifyCanExecuteChanged();
        MenuLogoutCommand.NotifyCanExecuteChanged();
    }

    private bool CanMenuLogout()
    {
        return _loginState >= LoginState.CONNECTED;
    }
}

