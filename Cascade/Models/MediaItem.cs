using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Cascade.Models
{
    /// <summary>
    /// 媒体项目模型，表示一个媒体文件
    /// </summary>
    public class MediaItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _format = string.Empty;
        private string _size = string.Empty;
        private string _duration = string.Empty;
        private string _resolution = string.Empty;
        private string _filePath = string.Empty;
        private ImageSource? _thumbnail;
        private string _videoCodec = string.Empty;
        private string _videoProfile = string.Empty;
        private string _videoLevel = string.Empty;
        private string _videoColorDepth = string.Empty;
        private string _videoBitrate = string.Empty;
        private string _audioCodec = string.Empty;
        private string _audioSampleRate = string.Empty;
        private string _audioBitDepth = string.Empty;
        private string _audioBitrate = string.Empty;
        private string _frameRate = string.Empty;
        private string _bitrate = string.Empty;
        private string _subtitles = string.Empty;
        private string _metadata = string.Empty;
        private bool _isLoading = false;

        /// <summary>
        /// 文件名称
        /// </summary>
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 文件格式 (如 MP4, MKV)
        /// </summary>
        public string Format
        {
            get => _format;
            set { if (_format != value) { _format = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 文件大小 (格式化后的字符串，如 10 MB)
        /// </summary>
        public string Size
        {
            get => _size;
            set { if (_size != value) { _size = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 持续时间 (格式化后的字符串，如 00:05:30)
        /// </summary>
        public string Duration
        {
            get => _duration;
            set { if (_duration != value) { _duration = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 分辨率 (如 1920x1080)
        /// </summary>
        public string Resolution
        {
            get => _resolution;
            set { if (_resolution != value) { _resolution = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 完整文件路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set { if (_filePath != value) { _filePath = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 缩略图
        /// </summary>
        public ImageSource? Thumbnail
        {
            get => _thumbnail;
            set { if (_thumbnail != value) { _thumbnail = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 视频编码
        /// </summary>
        public string VideoCodec
        {
            get => _videoCodec;
            set { if (_videoCodec != value) { _videoCodec = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 视频 Profile
        /// </summary>
        public string VideoProfile
        {
            get => _videoProfile;
            set { if (_videoProfile != value) { _videoProfile = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 视频 Level
        /// </summary>
        public string VideoLevel
        {
            get => _videoLevel;
            set { if (_videoLevel != value) { _videoLevel = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 视频量化精度（色深）
        /// </summary>
        public string VideoColorDepth
        {
            get => _videoColorDepth;
            set { if (_videoColorDepth != value) { _videoColorDepth = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 视频比特率
        /// </summary>
        public string VideoBitrate
        {
            get => _videoBitrate;
            set { if (_videoBitrate != value) { _videoBitrate = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 音频编码
        /// </summary>
        public string AudioCodec
        {
            get => _audioCodec;
            set { if (_audioCodec != value) { _audioCodec = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 音频采样率
        /// </summary>
        public string AudioSampleRate
        {
            get => _audioSampleRate;
            set { if (_audioSampleRate != value) { _audioSampleRate = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 音频量化精度（位深）
        /// </summary>
        public string AudioBitDepth
        {
            get => _audioBitDepth;
            set { if (_audioBitDepth != value) { _audioBitDepth = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 音频比特率
        /// </summary>
        public string AudioBitrate
        {
            get => _audioBitrate;
            set { if (_audioBitrate != value) { _audioBitrate = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 字幕信息
        /// </summary>
        public string Subtitles
        {
            get => _subtitles;
            set { if (_subtitles != value) { _subtitles = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 元数据信息
        /// </summary>
        public string Metadata
        {
            get => _metadata;
            set { if (_metadata != value) { _metadata = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 帧率
        /// </summary>
        public string FrameRate
        {
            get => _frameRate;
            set { if (_frameRate != value) { _frameRate = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 比特率
        /// </summary>
        public string Bitrate
        {
            get => _bitrate;
            set { if (_bitrate != value) { _bitrate = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 是否正在加载信息
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
