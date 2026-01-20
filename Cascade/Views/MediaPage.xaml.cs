using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Cascade.Models;
using Cascade.ViewModels;

namespace Cascade.Views
{
    /// <summary>
    /// 媒体页面 - 媒体管理和浏览功能
    /// </summary>
    public partial class MediaPage : UserControl
    {
        private MediaElement? _mediaElement;
        private DispatcherTimer? _timer;

        public MediaPage()
        {
            InitializeComponent();
            this.Loaded += MediaPage_Loaded;
            this.Unloaded += MediaPage_Unloaded;
        }

        // 简单的转换器，用于根据播放状态切换图标
        public class BoolToGeometryConverter : System.Windows.Data.IValueConverter
        {
            public Geometry TrueGeometry { get; set; }
            public Geometry FalseGeometry { get; set; }

            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is bool b && b)
                {
                    return TrueGeometry;
                }
                return FalseGeometry;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private void MediaPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 查找 MediaElement
            _mediaElement = FindVisualChild<MediaElement>(this);
            
            if (_mediaElement != null)
            {
                _mediaElement.MediaOpened += MediaElement_MediaOpened;
                _mediaElement.MediaEnded += MediaElement_MediaEnded;
                _mediaElement.MediaFailed += MediaElement_MediaFailed;
            }

            // 初始化定时器用于更新进度
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;

            // 订阅 ViewModel 事件
            if (DataContext is MediaViewModel vm)
            {
                SubscribeToPlayerEvents(vm.PlayerViewModel);
            }
            else if (DataContext is MainViewModel mainVm)
            {
                SubscribeToPlayerEvents(mainVm.MediaVM.PlayerViewModel);
            }
        }

        private void MediaPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }

            if (_mediaElement != null)
            {
                _mediaElement.MediaOpened -= MediaElement_MediaOpened;
                _mediaElement.MediaEnded -= MediaElement_MediaEnded;
                _mediaElement.MediaFailed -= MediaElement_MediaFailed;
            }

            // 取消订阅 ViewModel 事件
            if (DataContext is MediaViewModel vm)
            {
                UnsubscribeFromPlayerEvents(vm.PlayerViewModel);
            }
            else if (DataContext is MainViewModel mainVm)
            {
                UnsubscribeFromPlayerEvents(mainVm.MediaVM.PlayerViewModel);
            }
        }

        private void SubscribeToPlayerEvents(PlayerViewModel playerVm)
        {
            playerVm.PlayRequested += PlayerVm_PlayRequested;
            playerVm.PauseRequested += PlayerVm_PauseRequested;
            playerVm.StopRequested += PlayerVm_StopRequested;
            playerVm.SeekRequested += PlayerVm_SeekRequested;
        }

        private void UnsubscribeFromPlayerEvents(PlayerViewModel playerVm)
        {
            playerVm.PlayRequested -= PlayerVm_PlayRequested;
            playerVm.PauseRequested -= PlayerVm_PauseRequested;
            playerVm.StopRequested -= PlayerVm_StopRequested;
            playerVm.SeekRequested -= PlayerVm_SeekRequested;
        }

        private void PlayerVm_PlayRequested(object? sender, EventArgs e)
        {
            _mediaElement?.Play();
            _timer?.Start();
        }

        private void PlayerVm_PauseRequested(object? sender, EventArgs e)
        {
            _mediaElement?.Pause();
            _timer?.Stop();
        }

        private void PlayerVm_StopRequested(object? sender, EventArgs e)
        {
            _mediaElement?.Stop();
            _timer?.Stop();
        }

        private void PlayerVm_SeekRequested(object? sender, double position)
        {
            if (_mediaElement != null)
            {
                _mediaElement.Position = TimeSpan.FromSeconds(position);
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (_mediaElement != null && _mediaElement.NaturalDuration.HasTimeSpan)
            {
                var duration = _mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                if (DataContext is MediaViewModel vm)
                {
                    vm.PlayerViewModel.Duration = duration;
                    // 自动播放
                    vm.PlayerViewModel.IsPlaying = true;
                    _mediaElement.Play();
                    _timer?.Start();
                }
                else if (DataContext is MainViewModel mainVm)
                {
                    mainVm.MediaVM.PlayerViewModel.Duration = duration;
                    // 自动播放
                    mainVm.MediaVM.PlayerViewModel.IsPlaying = true;
                    _mediaElement.Play();
                    _timer?.Start();
                }
            }
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MediaViewModel vm)
            {
                vm.PlayerViewModel.IsPlaying = false;
                vm.PlayerViewModel.Position = 0;
            }
            else if (DataContext is MainViewModel mainVm)
            {
                mainVm.MediaVM.PlayerViewModel.IsPlaying = false;
                mainVm.MediaVM.PlayerViewModel.Position = 0;
            }
            _mediaElement?.Stop();
            _timer?.Stop();
        }

        private void MediaElement_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            // 处理播放失败
            MessageBox.Show($"无法播放媒体文件: {e.ErrorException.Message}", "播放错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_mediaElement != null)
            {
                if (DataContext is MediaViewModel vm)
                {
                    vm.PlayerViewModel.UpdatePosition(_mediaElement.Position.TotalSeconds);
                }
                else if (DataContext is MainViewModel mainVm)
                {
                    mainVm.MediaVM.PlayerViewModel.UpdatePosition(_mediaElement.Position.TotalSeconds);
                }
            }
        }

        // 辅助方法：查找可视子元素
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    if (DataContext is MediaViewModel viewModel)
                    {
                        viewModel.AddFiles(files);
                    }
                    // 如果 DataContext 是 MainViewModel，则需要访问其 MediaVM 属性
                    else if (DataContext is MainViewModel mainViewModel)
                    {
                        mainViewModel.MediaVM.AddFiles(files);
                    }
                }
            }
        }

        private bool _isSyncingSelection = false;

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;

            try
            {
                _isSyncingSelection = true;
                
                // 清除 ListView 的选择，保持互斥
                var mediaListView = this.FindName("MediaListView") as ListView;
                if (mediaListView != null && mediaListView.SelectedItems.Count > 0)
                {
                    mediaListView.SelectedItems.Clear();
                }

                UpdateSelectedItems(sender as ListBox);
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;

            try
            {
                _isSyncingSelection = true;

                // 清除 ListBox 的选择，保持互斥
                var mediaListBox = this.FindName("MediaListBox") as ListBox;
                if (mediaListBox != null && mediaListBox.SelectedItems.Count > 0)
                {
                    mediaListBox.SelectedItems.Clear();
                }

                UpdateSelectedItems(sender as ListView);
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void UpdateSelectedItems(ListBox? listBox)
        {
            if (listBox == null) return;

            MediaViewModel? viewModel = null;
            if (DataContext is MediaViewModel vm)
            {
                viewModel = vm;
            }
            else if (DataContext is MainViewModel mainVm)
            {
                viewModel = mainVm.MediaVM;
            }

            if (viewModel != null)
            {
                viewModel.SelectedMediaItems.Clear();
                foreach (var item in listBox.SelectedItems)
                {
                    if (item is MediaItem mediaItem)
                    {
                        viewModel.SelectedMediaItems.Add(mediaItem);
                    }
                }
                
                // 同时更新 SelectedMediaItem 为最后一个选中的项，以便详情页显示
                if (viewModel.SelectedMediaItems.Count > 0)
                {
                    viewModel.SelectedMediaItem = viewModel.SelectedMediaItems[viewModel.SelectedMediaItems.Count - 1];
                }
                else
                {
                    viewModel.SelectedMediaItem = null;
                }
            }
        }
    }
}
