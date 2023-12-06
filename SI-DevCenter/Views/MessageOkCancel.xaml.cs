using System.Windows;

namespace SI_DevCenter.Views
{
    /// <summary>
    /// Interaction logic for MessageOkCancel.xaml
    /// </summary>
    public partial class MessageOkCancel : Window
    {
        public MessageOkCancel(string Message, string Caption)
        {
            InitializeComponent();
            MsgText.Text = Message;
            Title = Caption;

            Owner = Application.Current.MainWindow;

        }

        private void btnOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancleClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
