namespace SI_DevCenter.Models
{
    internal class LoginInfo
    {
        public string UserID;
        public string Password;

        public LoginState State;

        public LoginInfo()
        {
            UserID = string.Empty;
            Password = string.Empty;
            State = LoginState.NONE;
        }
    }
}
