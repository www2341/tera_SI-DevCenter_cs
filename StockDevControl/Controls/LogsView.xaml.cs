using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StockDevControl.Controls
{
    /// <summary>
    /// Interaction logic for LogsView.xaml
    /// </summary>
    public partial class LogsView : TabControl
    {
        public object? DoubleClickedItem
        {
            get { return (object?)GetValue(DoubleClickedItemProperty); }
            set { SetValue(DoubleClickedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DoubleClickedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DoubleClickedItemProperty =
            DependencyProperty.Register("DoubleClickedItem", typeof(object), typeof(LogsView), new PropertyMetadata(null));

        public object? SelectedListItem
        {
            get { return (object?)GetValue(SelectedListItemProperty); }
            set { SetValue(SelectedListItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedListItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedListItemProperty =
            DependencyProperty.Register("SelectedListItem", typeof(object), typeof(LogsView), new PropertyMetadata(null));

        public LogsView()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetValue(DoubleClickedItemProperty, SelectedListItem);
        }

        private void ListBox_SelectedChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                SetValue(SelectedListItemProperty, e.AddedItems[0]);
        }
    }
}
