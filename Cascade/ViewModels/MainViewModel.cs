using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cascade.Models;
using Cascade.Services;

namespace Cascade.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _statusMessage = string.Empty;
        private int _selectedTabIndex = -1;
        private bool _isWarningMode = false;

        // 子页面 ViewModel 实例
        public WelcomeViewModel WelcomeVM { get; } = new WelcomeViewModel();
        public MediaViewModel MediaVM { get; } = new MediaViewModel();
        public OperationsViewModel OperationsVM { get; } = new OperationsViewModel();
        public TasksViewModel TasksVM { get; } = new TasksViewModel();
        public QueueViewModel QueueVM { get; } = new QueueViewModel();

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否为警告模式
        /// </summary>
        public bool IsWarningMode
        {
            get => _isWarningMode;
            set => SetProperty(ref _isWarningMode, value);
        }

        /// <summary>
        /// 当前选中的 Tab 索引
        /// -1 表示未选中任何 Tab (显示欢迎页面)
        /// 0 = Media, 1 = Operations, 2 = Tasks, 3 = Queue
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnTabChanged(value);
                }
            }
        }

        public MainViewModel()
        {
            StatusMessage = LocalizationService.GetString("Status_Ready");
            
            // 监听媒体列表变化（添加/删除媒体）
            MediaVM.MediaItems.CollectionChanged += OnMediaItemsChanged;
            
            // 订阅通知服务
            NotificationService.NotificationChanged += OnNotificationChanged;
        }

        /// <summary>
        /// 处理通知消息变更
        /// </summary>
        private void OnNotificationChanged(object? sender, NotificationMessage notification)
        {
            // 更新消息文本
            StatusMessage = notification.IsLocalizationKey 
                ? LocalizationService.GetString(notification.Message)
                : notification.Message;

            // 更新警告模式
            IsWarningMode = notification.Type == NotificationType.Warning;
        }

        /// <summary>
        /// 标签页切换时的处理
        /// </summary>
        private void OnTabChanged(int tabIndex)
        {
            // 当切换到任务页面时，同步数据
            if (tabIndex == 2) // Tasks tab
            {
                SyncTasksPageData();
            }
        }

        /// <summary>
        /// 媒体列表变化时的处理
        /// </summary>
        private void OnMediaItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 如果当前在任务页面，实时更新
            if (SelectedTabIndex == 2)
            {
                TasksVM.UpdateMediaItems(MediaVM.MediaItems);
            }
        }

        /// <summary>
        /// 同步任务页面数据
        /// </summary>
        private void SyncTasksPageData()
        {
            // 同步所有媒体项
            TasksVM.UpdateMediaItems(MediaVM.MediaItems);
            
            // 刷新预览
            TasksVM.RefreshPreview();
        }
    }
}