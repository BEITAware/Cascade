using System;
using System.Windows.Input;
using System.Windows.Threading;
using Cascade.Models;

namespace Cascade.ViewModels
{
    public class PlayerViewModel : ViewModelBase
    {
        private bool _isPlaying;
        private double _position;
        private double _duration;
        private double _volume = 0.5;
        private bool _isMuted;
        private MediaItem? _currentMediaItem;

        public PlayerViewModel()
        {
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            StopCommand = new RelayCommand(ExecuteStop);
            FastForwardCommand = new RelayCommand(ExecuteFastForward);
            RewindCommand = new RelayCommand(ExecuteRewind);
            NextCommand = new RelayCommand(ExecuteNext);
            ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreen);
            MuteCommand = new RelayCommand(ExecuteMute);
        }

        public MediaItem? CurrentMediaItem
        {
            get => _currentMediaItem;
            set
            {
                if (SetProperty(ref _currentMediaItem, value))
                {
                    // 重置播放状态
                    Position = 0;
                    IsPlaying = false;
                    // 实际加载逻辑由 View 层的 MediaElement 处理，或者通过 Behavior
                }
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public double Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public double Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    if (_volume > 0) IsMuted = false;
                }
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set => SetProperty(ref _isMuted, value);
        }

        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand FastForwardCommand { get; }
        public ICommand RewindCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }
        public ICommand MuteCommand { get; }

        // 事件，用于通知 View 层执行某些操作（如 MediaElement 的控制）
        public event EventHandler? PlayRequested;
        public event EventHandler? PauseRequested;
        public event EventHandler? StopRequested;
        public event EventHandler<double>? SeekRequested;

        private void ExecutePlayPause(object? parameter)
        {
            if (IsPlaying)
            {
                PauseRequested?.Invoke(this, EventArgs.Empty);
                IsPlaying = false;
            }
            else
            {
                PlayRequested?.Invoke(this, EventArgs.Empty);
                IsPlaying = true;
            }
        }

        private void ExecuteStop(object? parameter)
        {
            StopRequested?.Invoke(this, EventArgs.Empty);
            IsPlaying = false;
            Position = 0;
        }

        private void ExecuteFastForward(object? parameter)
        {
            // 快进 10 秒
            double newPos = Position + 10;
            if (newPos > Duration) newPos = Duration;
            SeekRequested?.Invoke(this, newPos);
        }

        private void ExecuteRewind(object? parameter)
        {
            // 快退 10 秒
            double newPos = Position - 10;
            if (newPos < 0) newPos = 0;
            SeekRequested?.Invoke(this, newPos);
        }

        private void ExecuteNext(object? parameter)
        {
            // 下一个视频逻辑，通常需要父 ViewModel 介入
            // 这里可以触发一个事件或者通过 Messenger 发送消息
        }

        private void ExecuteToggleFullScreen(object? parameter)
        {
            // 全屏逻辑，通常涉及 View 的 WindowState 改变
        }

        private void ExecuteMute(object? parameter)
        {
            IsMuted = !IsMuted;
            if (IsMuted)
            {
                // 记住当前音量或设为0? 通常 IsMuted 绑定到 MediaElement.IsMuted
            }
        }
        
        public void UpdatePosition(double position)
        {
            _position = position;
            OnPropertyChanged(nameof(Position));
        }
    }
}
