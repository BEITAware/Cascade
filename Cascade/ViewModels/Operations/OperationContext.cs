using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cascade.Services.CommandBuilders;

namespace Cascade.ViewModels.Operations
{
    /// <summary>
    /// 视频流处理模式枚举
    /// </summary>
    public enum VideoStreamMode
    {
        /// <summary>
        /// 编码模式 - 重新编码视频流
        /// </summary>
        Encode,
        
        /// <summary>
        /// 复制模式 - 直接复制视频流
        /// </summary>
        Copy
    }

    /// <summary>
    /// 页面共享数据变更事件参数
    /// </summary>
    public class SharedDataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 发布数据的页面ID
        /// </summary>
        public string PageId { get; }
        
        /// <summary>
        /// 变更的键名，null表示整个字典被替换
        /// </summary>
        public string? Key { get; }
        
        /// <summary>
        /// 新值，null表示键被移除
        /// </summary>
        public object? NewValue { get; }

        public SharedDataChangedEventArgs(string pageId, string? key = null, object? newValue = null)
        {
            PageId = pageId;
            Key = key;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// 操作上下文，用于子页面间共享状态和通信。
    /// 实现单例模式，所有子页面共享同一实例。
    /// 支持动态字典共享，允许页面发布和订阅任意数据。
    /// 支持多后端架构，可以在不同的命令构建器之间切换。
    /// </summary>
    public class OperationContext : INotifyPropertyChanged
    {
        private static OperationContext? _instance;
        private static readonly object _lock = new object();

        private VideoStreamMode _videoStreamMode = VideoStreamMode.Encode;
        private string? _selectedVideoEncoder;
        private string? _selectedAudioEncoder;
        private BackendType _selectedBackend = BackendType.FFmpeg;
        private readonly CommandBuilderFactory _commandBuilderFactory;

        // 页面共享数据存储：PageId -> (Key -> Value)
        private readonly Dictionary<string, Dictionary<string, object>> _sharedData = new();
        private readonly object _sharedDataLock = new();

        /// <summary>
        /// 当共享数据发生变更时触发
        /// </summary>
        public event EventHandler<SharedDataChangedEventArgs>? SharedDataChanged;
        
        /// <summary>
        /// 后端变更事件
        /// </summary>
        public event EventHandler<BackendType>? BackendChanged;

        /// <summary>
        /// 获取OperationContext的单例实例
        /// </summary>
        public static OperationContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new OperationContext();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private OperationContext()
        {
            _commandBuilderFactory = new CommandBuilderFactory();
        }

        #region 动态共享数据 API

        /// <summary>
        /// 发布或更新页面的共享数据
        /// </summary>
        /// <param name="pageId">页面ID</param>
        /// <param name="key">数据键</param>
        /// <param name="value">数据值</param>
        public void PublishData(string pageId, string key, object value)
        {
            lock (_sharedDataLock)
            {
                if (!_sharedData.TryGetValue(pageId, out var pageData))
                {
                    pageData = new Dictionary<string, object>();
                    _sharedData[pageId] = pageData;
                }
                pageData[key] = value;
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[OperationContext.PublishData] {pageId}.{key} = {value} (type: {value?.GetType().Name ?? "null"})");
#endif
            SharedDataChanged?.Invoke(this, new SharedDataChangedEventArgs(pageId, key, value));
        }

        /// <summary>
        /// 批量发布页面的共享数据（替换整个字典）
        /// </summary>
        /// <param name="pageId">页面ID</param>
        /// <param name="data">数据字典</param>
        public void PublishData(string pageId, Dictionary<string, object> data)
        {
            lock (_sharedDataLock)
            {
                _sharedData[pageId] = new Dictionary<string, object>(data);
            }
            SharedDataChanged?.Invoke(this, new SharedDataChangedEventArgs(pageId));
        }

        /// <summary>
        /// 移除页面的某个共享数据
        /// </summary>
        /// <param name="pageId">页面ID</param>
        /// <param name="key">数据键</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveData(string pageId, string key)
        {
            bool removed;
            lock (_sharedDataLock)
            {
                if (_sharedData.TryGetValue(pageId, out var pageData))
                {
                    removed = pageData.Remove(key);
                }
                else
                {
                    removed = false;
                }
            }
            if (removed)
            {
                SharedDataChanged?.Invoke(this, new SharedDataChangedEventArgs(pageId, key, null));
            }
            return removed;
        }

        /// <summary>
        /// 清除页面的所有共享数据
        /// </summary>
        /// <param name="pageId">页面ID</param>
        public void ClearPageData(string pageId)
        {
            lock (_sharedDataLock)
            {
                _sharedData.Remove(pageId);
            }
            SharedDataChanged?.Invoke(this, new SharedDataChangedEventArgs(pageId));
        }

        /// <summary>
        /// 获取指定页面的某个共享数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="pageId">页面ID</param>
        /// <param name="key">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值，如果不存在则返回默认值</returns>
        public T? GetData<T>(string pageId, string key, T? defaultValue = default)
        {
            lock (_sharedDataLock)
            {
                if (_sharedData.TryGetValue(pageId, out var pageData) &&
                    pageData.TryGetValue(key, out var value) &&
                    value is T typedValue)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[OperationContext.GetData] {pageId}.{key} = {typedValue} (type: {typeof(T).Name})");
#endif
                    return typedValue;
                }
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[OperationContext.GetData] {pageId}.{key} NOT FOUND, returning default: {defaultValue}");
#endif
            return defaultValue;
        }

        /// <summary>
        /// 尝试获取指定页面的某个共享数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="pageId">页面ID</param>
        /// <param name="key">数据键</param>
        /// <param name="value">输出值</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetData<T>(string pageId, string key, out T? value)
        {
            lock (_sharedDataLock)
            {
                if (_sharedData.TryGetValue(pageId, out var pageData) &&
                    pageData.TryGetValue(key, out var rawValue) &&
                    rawValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 获取指定页面的所有共享数据（只读副本）
        /// </summary>
        /// <param name="pageId">页面ID</param>
        /// <returns>数据字典副本，如果页面不存在则返回空字典</returns>
        public IReadOnlyDictionary<string, object> GetPageData(string pageId)
        {
            lock (_sharedDataLock)
            {
                if (_sharedData.TryGetValue(pageId, out var pageData))
                {
                    return new Dictionary<string, object>(pageData);
                }
            }
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取所有页面的共享数据（只读副本）
        /// </summary>
        /// <returns>所有页面数据的字典</returns>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetAllSharedData()
        {
            lock (_sharedDataLock)
            {
                var result = new Dictionary<string, IReadOnlyDictionary<string, object>>();
                foreach (var kvp in _sharedData)
                {
                    result[kvp.Key] = new Dictionary<string, object>(kvp.Value);
                }
                return result;
            }
        }

        /// <summary>
        /// 跨页面搜索数据：查找所有包含指定键的页面
        /// </summary>
        /// <param name="key">数据键</param>
        /// <returns>包含该键的页面ID和对应值的字典</returns>
        public IReadOnlyDictionary<string, object> FindDataByKey(string key)
        {
            var result = new Dictionary<string, object>();
            lock (_sharedDataLock)
            {
                foreach (var kvp in _sharedData)
                {
                    if (kvp.Value.TryGetValue(key, out var value))
                    {
                        result[kvp.Key] = value;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取所有已发布数据的页面ID列表
        /// </summary>
        /// <returns>页面ID列表</returns>
        public IReadOnlyList<string> GetPublishingPages()
        {
            lock (_sharedDataLock)
            {
                return new List<string>(_sharedData.Keys);
            }
        }

        #endregion

        #region 多后端支持

        /// <summary>
        /// 当前选择的后端
        /// </summary>
        public BackendType SelectedBackend
        {
            get => _selectedBackend;
            set
            {
                if (_selectedBackend != value)
                {
                    _selectedBackend = value;
                    OnPropertyChanged();
                    BackendChanged?.Invoke(this, value);
                }
            }
        }
        
        /// <summary>
        /// 获取当前后端的命令构建器
        /// </summary>
        /// <returns>当前后端的命令提供者</returns>
        public ICommandProvider GetCurrentCommandBuilder()
        {
            return _commandBuilderFactory.CreateBuilder(SelectedBackend);
        }
        
        /// <summary>
        /// 获取所有支持的后端
        /// </summary>
        /// <returns>支持的后端类型列表</returns>
        public IReadOnlyList<BackendType> GetSupportedBackends()
        {
            return _commandBuilderFactory.GetSupportedBackends();
        }
        
        /// <summary>
        /// 检查页面是否被当前后端支持
        /// </summary>
        /// <param name="pageId">页面ID</param>
        /// <returns>是否支持</returns>
        public bool IsPageSupportedByCurrentBackend(string pageId)
        {
            try
            {
                var builder = GetCurrentCommandBuilder();
                return builder.GetSupportedPages().Contains(pageId);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 生成当前后端的命令
        /// </summary>
        /// <returns>命令结果</returns>
        public CommandResult GenerateCommand()
        {
            var builder = GetCurrentCommandBuilder();
            return builder.GenerateCommand(this);
        }
        
        /// <summary>
        /// 验证当前后端的配置
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateCurrentConfiguration()
        {
            var builder = GetCurrentCommandBuilder();
            return builder.ValidateConfiguration(this);
        }
        
        /// <summary>
        /// 获取当前后端的预览文本
        /// </summary>
        /// <returns>预览文本</returns>
        public string GetPreviewText()
        {
            var builder = GetCurrentCommandBuilder();
            return builder.GetPreview(this);
        }

        #endregion

        /// <summary>
        /// 视频流处理模式（编码/复制）
        /// </summary>
        public VideoStreamMode VideoStreamMode
        {
            get => _videoStreamMode;
            set
            {
                if (_videoStreamMode != value)
                {
                    _videoStreamMode = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 选中的视频编码器
        /// </summary>
        public string? SelectedVideoEncoder
        {
            get => _selectedVideoEncoder;
            set
            {
                if (_selectedVideoEncoder != value)
                {
                    _selectedVideoEncoder = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 选中的音频编码器
        /// </summary>
        public string? SelectedAudioEncoder
        {
            get => _selectedAudioEncoder;
            set
            {
                if (_selectedAudioEncoder != value)
                {
                    _selectedAudioEncoder = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 重置实例（仅用于测试）
        /// </summary>
        internal static void ResetForTesting()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        #region 预设管理支持

        /// <summary>
        /// 导出当前状态为字典（用于预设保存）
        /// </summary>
        /// <returns>包含所有状态的字典</returns>
        public Dictionary<string, object> ExportState()
        {
            var state = new Dictionary<string, object>
            {
                ["VideoStreamMode"] = _videoStreamMode.ToString(),
                ["SelectedVideoEncoder"] = _selectedVideoEncoder ?? string.Empty,
                ["SelectedAudioEncoder"] = _selectedAudioEncoder ?? string.Empty,
                ["SelectedBackend"] = _selectedBackend.ToString()
            };

            // 导出所有共享数据
            lock (_sharedDataLock)
            {
                var sharedDataCopy = new Dictionary<string, Dictionary<string, object>>();
                foreach (var kvp in _sharedData)
                {
                    sharedDataCopy[kvp.Key] = new Dictionary<string, object>(kvp.Value);
                }
                state["SharedData"] = sharedDataCopy;
            }

            return state;
        }

        /// <summary>
        /// 从字典导入状态（用于预设加载）
        /// </summary>
        /// <param name="state">包含状态的字典</param>
        public void ImportState(Dictionary<string, object> state)
        {
            if (state.TryGetValue("VideoStreamMode", out var videoStreamModeObj) && 
                videoStreamModeObj is string videoStreamModeStr &&
                Enum.TryParse<VideoStreamMode>(videoStreamModeStr, out var videoStreamMode))
            {
                VideoStreamMode = videoStreamMode;
            }

            if (state.TryGetValue("SelectedVideoEncoder", out var videoEncoderObj) && 
                videoEncoderObj is string videoEncoder)
            {
                SelectedVideoEncoder = string.IsNullOrEmpty(videoEncoder) ? null : videoEncoder;
            }

            if (state.TryGetValue("SelectedAudioEncoder", out var audioEncoderObj) && 
                audioEncoderObj is string audioEncoder)
            {
                SelectedAudioEncoder = string.IsNullOrEmpty(audioEncoder) ? null : audioEncoder;
            }

            if (state.TryGetValue("SelectedBackend", out var backendObj) && 
                backendObj is string backendStr &&
                Enum.TryParse<BackendType>(backendStr, out var backend))
            {
                SelectedBackend = backend;
            }

            // 导入共享数据
            if (state.TryGetValue("SharedData", out var sharedDataObj))
            {
                Dictionary<string, Dictionary<string, object>>? sharedData = null;
                
                // 尝试直接转换
                if (sharedDataObj is Dictionary<string, Dictionary<string, object>> directDict)
                {
                    sharedData = directDict;
                }
                // 如果是Dictionary<string, object>，需要转换内部字典
                else if (sharedDataObj is Dictionary<string, object> outerDict)
                {
                    sharedData = new Dictionary<string, Dictionary<string, object>>();
                    foreach (var kvp in outerDict)
                    {
                        if (kvp.Value is Dictionary<string, object> innerDict)
                        {
                            sharedData[kvp.Key] = innerDict;
                        }
                    }
                }
                
                if (sharedData != null && sharedData.Count > 0)
                {
                    lock (_sharedDataLock)
                    {
                        _sharedData.Clear();
                        foreach (var kvp in sharedData)
                        {
                            _sharedData[kvp.Key] = new Dictionary<string, object>(kvp.Value);
                        }
                    }
                    // 触发所有页面的数据变更事件
                    foreach (var pageId in sharedData.Keys)
                    {
                        SharedDataChanged?.Invoke(this, new SharedDataChangedEventArgs(pageId));
                    }
                }
            }
        }

        #endregion
    }
}
