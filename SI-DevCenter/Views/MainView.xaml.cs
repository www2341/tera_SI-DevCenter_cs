using System.Windows;
using System.Windows.Input;

namespace SI_DevCenter.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            MouseDown += (sender, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    DragMove();
            };
        }
    }
}
