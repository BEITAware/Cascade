using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Cascade.Services;

namespace Cascade.ViewModels
{
    public class QueueViewModel : ViewModelBase
    {
        private ObservableCollection<QueueItemViewModel> _queueItems;
        private QueueItemViewModel? _selectedItem;
        private ObservableCollection<object> _selectedItems;
        private readonly QueueService _queueService;

        public ObservableCollection<QueueItemViewModel> QueueItems
        {
            get => _queueItems;
            set => SetProperty(ref _queueItems, value);
        }

        public QueueItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                // 更新命令的可执行状态
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<object> SelectedItems
        {
            get => _selectedItems;
            set
            {
                SetProperty(ref _selectedItems, value);
                // 更新命令的可执行状态
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearCompletedCommand { get; }
        public ICommand SettingsCommand { get; }

        public QueueViewModel()
        {
            _queueItems = new ObservableCollection<QueueItemViewModel>();
            _selectedItems = new ObservableCollection<object>();
            _queueService = QueueService.Instance;

            StartCommand = new RelayCommand(OnStart);
            PauseCommand = new RelayCommand(OnPause);
            MoveUpCommand = new RelayCommand(OnMoveUp, CanMoveUp);
            MoveDownCommand = new RelayCommand(OnMoveDown, CanMoveDown);
            RemoveCommand = new RelayCommand(OnRemove, CanRemove);
            ClearCompletedCommand = new RelayCommand(OnClearCompleted);
            SettingsCommand = new RelayCommand(OnSettings);

            // 订阅队列服务事件
            _queueService.TaskStatusChanged += OnTaskStatusChanged;
            _queueService.TaskProgressUpdated += OnTaskProgressUpdated;

            // 监听选中项变化
            _selectedItems.CollectionChanged += (s, e) => CommandManager.InvalidateRequerySuggested();

            // 加载现有任务
            RefreshQueueItems();
        }

        /// <summary>
        /// 刷新队列项
        /// </summary>
        public void RefreshQueueItems()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                QueueItems.Clear();
                var tasks = _queueService.GetAllTasks();
                foreach (var task in tasks)
                {
                    var item = new QueueItemViewModel
                    {
                        TaskId = task.Id,
                        Name = task.MediaItem.Name,
                        Status = GetLocalizedStatus(task.Status),
                        Progress = task.Progress,
                        Format = task.MediaItem.Format,
                        Size = task.MediaItem.Size,
                        Thumbnail = task.MediaItem.Thumbnail
                    };

                    // 设置时间显示
                    if (task.Status == QueueTaskStatus.Processing && task.EstimatedTimeRemaining.HasValue)
                    {
                        // 正在处理：显示预估剩余时间
                        var remaining = task.EstimatedTimeRemaining.Value;
                        item.EstimatedTime = remaining.TotalHours >= 1 
                            ? $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}"
                            : $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                        item.Duration = Services.LocalizationService.GetString("Queue_EstimatedRemaining") + ": " + item.EstimatedTime;
                    }
                    else if (task.EndTime.HasValue && task.StartTime.HasValue)
                    {
                        // 已完成：显示实际耗时
                        var elapsed = task.EndTime.Value - task.StartTime.Value;
                        item.Duration = elapsed.TotalHours >= 1
                            ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                    }
                    else if (task.StartTime.HasValue)
                    {
                        // 正在处理但没有预估时间：显示已用时间
                        var elapsed = DateTime.Now - task.StartTime.Value;
                        item.Duration = elapsed.TotalHours >= 1
                            ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                    }
                    else
                    {
                        // 等待中
                        item.Duration = "--:--:--";
                    }

                    QueueItems.Add(item);
                }
            });
        }

        private void OnTaskStatusChanged(object? sender, QueueTask task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var item = QueueItems.FirstOrDefault(i => i.TaskId == task.Id);
                if (item != null)
                {
                    item.Status = GetLocalizedStatus(task.Status);
                    item.Progress = task.Progress;
                }
                else
                {
                    RefreshQueueItems();
                }
            });
        }

        private void OnTaskProgressUpdated(object? sender, QueueTask task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var item = QueueItems.FirstOrDefault(i => i.TaskId == task.Id);
                if (item != null)
                {
                    item.Progress = task.Progress;
                    
                    // 更新预估时间
                    if (task.EstimatedTimeRemaining.HasValue)
                    {
                        var remaining = task.EstimatedTimeRemaining.Value;
                        item.EstimatedTime = remaining.TotalHours >= 1 
                            ? $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}"
                            : $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                        item.Duration = Services.LocalizationService.GetString("Queue_EstimatedRemaining") + ": " + item.EstimatedTime;
                    }
                    else if (task.StartTime.HasValue)
                    {
                        // 显示已用时间
                        var elapsed = DateTime.Now - task.StartTime.Value;
                        item.Duration = elapsed.TotalHours >= 1
                            ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                    }
                }
            });
        }

        private string GetLocalizedStatus(QueueTaskStatus status)
        {
            return status switch
            {
                QueueTaskStatus.Waiting => Services.LocalizationService.GetString("Queue_Status_Waiting"),
                QueueTaskStatus.Processing => Services.LocalizationService.GetString("Queue_Status_Processing"),
                QueueTaskStatus.Completed => Services.LocalizationService.GetString("Queue_Status_Completed"),
                QueueTaskStatus.Failed => Services.LocalizationService.GetString("Queue_Status_Failed"),
                QueueTaskStatus.Cancelled => Services.LocalizationService.GetString("Queue_Status_Paused"),
                _ => status.ToString()
            };
        }

        private void OnStart()
        {
            if (_queueService.IsRunning)
            {
                _queueService.Pause();
            }
            else
            {
                _queueService.Start();
            }
        }

        private void OnPause()
        {
            _queueService.Pause();
        }

        private bool CanMoveUp()
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
                return false;

            // 检查所有选中项是否都可以上移
            var selectedTaskIds = SelectedItems.OfType<QueueItemViewModel>().Select(i => i.TaskId).ToList();
            if (selectedTaskIds.Count == 0)
                return false;

            // 获取所有选中项的索引
            var indices = selectedTaskIds.Select(id => 
            {
                var item = QueueItems.FirstOrDefault(i => i.TaskId == id);
                return item != null ? QueueItems.IndexOf(item) : -1;
            }).Where(i => i >= 0).OrderBy(i => i).ToList();

            // 如果第一个选中项已经在顶部，则不能上移
            return indices.Count > 0 && indices[0] > 0;
        }

        private void OnMoveUp()
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
                return;

            var selectedTaskIds = SelectedItems.OfType<QueueItemViewModel>()
                .Select(i => i.TaskId)
                .ToList();

            if (selectedTaskIds.Count == 0)
                return;

            // 按当前顺序排序（从上到下）
            var orderedIds = selectedTaskIds
                .Select(id => new { Id = id, Index = QueueItems.IndexOf(QueueItems.First(i => i.TaskId == id)) })
                .OrderBy(x => x.Index)
                .Select(x => x.Id)
                .ToList();

            // 调用服务层方法
            var movedCount = _queueService.MoveTasksUp(orderedIds);

            if (movedCount > 0)
            {
                // 刷新列表
                RefreshQueueItems();
                
                // 恢复选中状态
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedItems.Clear();
                    foreach (var id in selectedTaskIds)
                    {
                        var item = QueueItems.FirstOrDefault(i => i.TaskId == id);
                        if (item != null)
                        {
                            SelectedItems.Add(item);
                        }
                    }
                });
            }
        }

        private bool CanMoveDown()
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
                return false;

            var selectedTaskIds = SelectedItems.OfType<QueueItemViewModel>().Select(i => i.TaskId).ToList();
            if (selectedTaskIds.Count == 0)
                return false;

            // 获取所有选中项的索引
            var indices = selectedTaskIds.Select(id => 
            {
                var item = QueueItems.FirstOrDefault(i => i.TaskId == id);
                return item != null ? QueueItems.IndexOf(item) : -1;
            }).Where(i => i >= 0).OrderByDescending(i => i).ToList();

            // 如果最后一个选中项已经在底部，则不能下移
            return indices.Count > 0 && indices[0] < QueueItems.Count - 1;
        }

        private void OnMoveDown()
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
                return;

            var selectedTaskIds = SelectedItems.OfType<QueueItemViewModel>()
                .Select(i => i.TaskId)
                .ToList();

            if (selectedTaskIds.Count == 0)
                return;

            // 按当前顺序排序（从下到上）
            var orderedIds = selectedTaskIds
                .Select(id => new { Id = id, Index = QueueItems.IndexOf(QueueItems.First(i => i.TaskId == id)) })
                .OrderByDescending(x => x.Index)
                .Select(x => x.Id)
                .ToList();

            // 调用服务层方法
            var movedCount = _queueService.MoveTasksDown(orderedIds);

            if (movedCount > 0)
            {
                // 刷新列表
                RefreshQueueItems();
                
                // 恢复选中状态
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedItems.Clear();
                    foreach (var id in selectedTaskIds)
                    {
                        var item = QueueItems.FirstOrDefault(i => i.TaskId == id);
                        if (item != null)
                        {
                            SelectedItems.Add(item);
                        }
                    }
                });
            }
        }

        private bool CanRemove()
        {
            return SelectedItems != null && SelectedItems.Count > 0;
        }

        private void OnRemove()
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
                return;

            var selectedTaskIds = SelectedItems.OfType<QueueItemViewModel>()
                .Select(i => i.TaskId)
                .ToList();

            if (selectedTaskIds.Count == 0)
                return;

            // 确认删除
            var message = selectedTaskIds.Count == 1
                ? Services.LocalizationService.GetString("Queue_ConfirmRemove_Single")
                : string.Format(Services.LocalizationService.GetString("Queue_ConfirmRemove_Multiple"), selectedTaskIds.Count);

            var result = MessageBox.Show(
                message,
                Services.LocalizationService.GetString("Queue_ConfirmRemove_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var removedCount = _queueService.RemoveTasks(selectedTaskIds);
                
                if (removedCount > 0)
                {
                    RefreshQueueItems();
                    NotificationService.SendInformation(
                        string.Format(Services.LocalizationService.GetString("Queue_TasksRemoved"), removedCount),
                        false);
                }
            }
        }

        private void OnClearCompleted()
        {
            _queueService.ClearCompletedTasks();
            RefreshQueueItems();
        }

        private void OnSettings()
        {
            // TODO: 打开设置对话框以配置最大并行任务数等
        }
    }

    public class QueueItemViewModel : ViewModelBase
    {
        private string _taskId;
        private string _name;
        private string _status;
        private double _progress;
        private string _progressText;
        private string _format;
        private string _size;
        private string _duration;
        private string _estimatedTime;
        private object _thumbnail;

        public string TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public double Progress
        {
            get => _progress;
            set
            {
                SetProperty(ref _progress, value);
                ProgressText = $"{value:F0}%";
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public string Format
        {
            get => _format;
            set => SetProperty(ref _format, value);
        }

        public string Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public string Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        public string EstimatedTime
        {
            get => _estimatedTime;
            set => SetProperty(ref _estimatedTime, value);
        }

        public object Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }
    }
}
