
using SI.Component.Models;
using System.Windows.Controls;

namespace SI.Component.Components
{
    /// <summary>
    /// Interaction logic for ChartReqComponent.xaml
    /// </summary>
    public partial class ChartReqComponent : UserControl, IUserTool
    {
        public ChartReqComponent(object ComponentModel)
        {
            InitializeComponent();
            DataContext = ComponentModel;
            if (DataContext is ChartReqModel model)
            {
                model.EnableUpdateCodeText = true;
            }
        }

        public void CloseTool()
        {
            if (DataContext is ChartReqModel model)
            {
                model.EnableUpdateCodeText = false;
            }
        }
    }
}
