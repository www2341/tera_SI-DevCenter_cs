using System.Windows;

namespace SI_DevCenter.Views
{
    /// <summary>
    /// Interaction logic for KeySettings.xaml
    /// </summary>
    public partial class KeySettings : Window
    {
        public KeySettings(string UserID, string Password)
        {
            InitializeComponent();

            this.UserID.Text = UserID;
            this.Password.Password = Password;

            Owner = Application.Current.MainWindow;
            Topmost = Owner.Topmost;
        }

        private void BtnOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
