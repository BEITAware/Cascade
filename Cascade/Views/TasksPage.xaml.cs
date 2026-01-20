using System.Windows.Controls;
using Cascade.ViewModels;

namespace Cascade.Views
{
    /// <summary>
    /// 任务页面 - 预览选中的媒体和操作设定
    /// </summary>
    public partial class TasksPage : UserControl
    {
        public TasksPage()
        {
            InitializeComponent();
            // DataContext由MainWindow通过绑定设置，不在这里创建新实例
        }

        /// <summary>
        /// 获取TasksViewModel实例
        /// </summary>
        public TasksViewModel? ViewModel => DataContext as TasksViewModel;
    }
}
