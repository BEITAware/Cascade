using System.Windows.Controls;
using Cascade.ViewModels;

namespace Cascade.Views
{
    /// <summary>
    /// 队列页面 - 任务队列管理功能
    /// </summary>
    public partial class QueuePage : UserControl
    {
        public QueuePage()
        {
            InitializeComponent();
            DataContext = new QueueViewModel();
        }
    }
}
