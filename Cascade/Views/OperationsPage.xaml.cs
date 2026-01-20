using System.Windows.Controls;
using System.Windows.Input;
using Cascade.ViewModels;
using Cascade.ViewModels.Operations;

namespace Cascade.Views
{
    /// <summary>
    /// 操作页面 - 各种操作和编辑功能
    /// </summary>
    public partial class OperationsPage : UserControl
    {
        public OperationsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 处理子页面项点击事件
        /// </summary>
        private void SubPageItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && 
                element.DataContext is SelectableSubPageViewModel subPageViewModel &&
                DataContext is OperationsViewModel viewModel)
            {
                viewModel.SelectedSubPageViewModel = subPageViewModel;
            }
        }
    }
}
