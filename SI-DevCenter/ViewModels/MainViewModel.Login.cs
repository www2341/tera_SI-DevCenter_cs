using CommunityToolkit.Mvvm.Input;
using SI_DevCenter.Models;
using SI_DevCenter.Views;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    [RelayCommand(CanExecute = nameof(CanMenuLogin))]
    private void MenuLogin()
    {
        if (_loginState == LoginState.CREATED)
        {
            _axHDFCommAgent.CommSetOCXPath("D:\\EZ\\SIAPI");
            int ret = _axHDFCommAgent.CommInit(1);
            if (ret == 0)
            {
                StatusText = "통신관리자 실행 성공";
                _loginState = LoginState.CONNECTED;
            }
            else
                StatusText = $"통신관리자 실행 오류: {ret}";
        }

        if (_loginState == LoginState.CONNECTED)
        {
            _ = LoadTRListsAsync();

            // 통신관리자 연결 되었다면, 사용자 아이디, 페스워드 로그인
            string UserID = _appRegistry.GetValue("InitData", "UserID", string.Empty);
            string Password = _appRegistry.GetValue("InitData", "Password", string.Empty);
            string CertPassword = _appRegistry.GetValue("InitData", "CertPassword", string.Empty);

            var keySetting = new KeySettings(UserID, Password, CertPassword);
            if (keySetting.ShowDialog() == true)
            {
                UserID = keySetting.UserID.Text;
                Password = keySetting.Password.Password;
                CertPassword = keySetting.CertPassword.Password;

                _appRegistry.SetValue("InitData", "UserID", UserID);
                _appRegistry.SetValue("InitData", "Password", Password);
                _appRegistry.SetValue("InitData", "CertPassword", CertPassword);

                int ret = _axHDFCommAgent.CommLogin(UserID, Password, CertPassword);
                if (ret > 0)
                {
                    StatusText = "로그인 성공";
                    _loginState = LoginState.LOGINED;
                }
                else
                {
                    StatusText = $"로그인 실패: {ret}";
                }

            }
        }

        MenuLoginCommand.NotifyCanExecuteChanged();
        MenuLogoutCommand.NotifyCanExecuteChanged();
    }

    private bool CanMenuLogin()
    {
        return _loginState != LoginState.NONE && _loginState != LoginState.LOGINED;
    }

    [RelayCommand(CanExecute = nameof(CanMenuLogout))]
    private void MenuLogout()
    {
        _axHDFCommAgent.CommTerminate(1);
        _loginState = LoginState.CREATED;
        StatusText = "통신관리자 종료";

        MenuLoginCommand.NotifyCanExecuteChanged();
        MenuLogoutCommand.NotifyCanExecuteChanged();
    }

    private bool CanMenuLogout()
    {
        return _loginState >= LoginState.CONNECTED;
    }
}

