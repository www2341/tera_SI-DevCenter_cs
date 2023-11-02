using System.Windows;

namespace SI_DevCenter.Views
{
    /// <summary>
    /// Interaction logic for KeySettings.xaml
    /// </summary>
    public partial class KeySettings : Window
    {
        public KeySettings(string UserID, string Password, string CertPassword)
        {
            InitializeComponent();

            this.UserID.Text = UserID;
            this.Password.Password = Password;
            this.CertPassword.Password = CertPassword;
        }

        private void btnOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
