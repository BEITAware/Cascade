using System.Windows.Controls;
using Cascade.ViewModels.Operations.CascadeIO;

namespace Cascade.Views.Operations.CascadeIO
{
    /// <summary>
    /// NamingAndOutputView.xaml 的交互逻辑
    /// </summary>
    public partial class NamingAndOutputView : UserControl
    {
        public NamingAndOutputView()
        {
            InitializeComponent();
            DataContext = new NamingAndOutputViewModel();
        }
    }
}
