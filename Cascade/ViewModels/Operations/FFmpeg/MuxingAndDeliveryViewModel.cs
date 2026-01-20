using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Cascade.ViewModels.Operations;
using Cascade.Services;

namespace Cascade.ViewModels.Operations.FFmpeg
{
    /// <summary>
    /// 封装与交付子页面ViewModel
    /// </summary>
    public class MuxingAndDeliveryViewModel : OperationViewModelBase
    {
        public override string SubPageId => "ffmpeg-muxing-delivery";

        #region 格式与容器

        public ObservableCollection<string> AvailableContainers { get; } = new()
        {
            "MKV",
            "MP4",
            "QuickTime",
            "TS",
            "FLV"
        };

        private int _selectedContainerIndex;
        public int SelectedContainerIndex
        {
            get => _selectedContainerIndex;
            set
            {
                if (SetProperty(ref _selectedContainerIndex, value))
                {
                    OnPropertyChanged(nameof(IsWebOptimizedEnabled));
                    OnPropertyChanged(nameof(IsHeaderCompressionEnabled));
                    
                    // 当容器改变时，如果选项不可用则重置
                    if (!IsWebOptimizedEnabled)
                        IsWebOptimized = false;
                    if (!IsHeaderCompressionEnabled)
                        IsHeaderCompression = false;
                }
            }
        }

        #endregion

        #region 网络优化

        private bool _isWebOptimized;
        public bool IsWebOptimized
        {
            get => _isWebOptimized;
            set => SetProperty(ref _isWebOptimized, value);
        }

        /// <summary>
        /// 仅在容器为MP4时启用
        /// </summary>
        public bool IsWebOptimizedEnabled => SelectedContainerIndex == 1; // MP4

        #endregion

        #region 头部压缩

        private bool _isHeaderCompression;
        public bool IsHeaderCompression
        {
            get => _isHeaderCompression;
            set => SetProperty(ref _isHeaderCompression, value);
        }

        /// <summary>
        /// 仅在容器为MKV时启用
        /// </summary>
        public bool IsHeaderCompressionEnabled => SelectedContainerIndex == 0; // MKV

        #endregion

        public MuxingAndDeliveryViewModel()
        {
            // 默认选择MKV
            SelectedContainerIndex = 0;

            LocalizationService.LanguageChanged += OnLanguageChanged;
            
            // 发布初始值到OperationContext
            PublishInitialValues();
        }

        private void PublishInitialValues()
        {
            PublishData(nameof(SelectedContainerIndex), SelectedContainerIndex);
            PublishData(nameof(IsWebOptimized), IsWebOptimized);
            PublishData(nameof(IsHeaderCompression), IsHeaderCompression);
        }

        private void OnLanguageChanged(object? sender, string e)
        {
            // 触发相关属性更新
        }

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["selectedContainerIndex"] = SelectedContainerIndex,
                ["isWebOptimized"] = IsWebOptimized,
                ["isHeaderCompression"] = IsHeaderCompression
            };
        }

        public override void Deserialize(JsonObject data)
        {
            if (data.TryGetPropertyValue("selectedContainerIndex", out var sci))
                SelectedContainerIndex = sci?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("isWebOptimized", out var iwo))
                IsWebOptimized = iwo?.GetValue<bool>() ?? false;
            if (data.TryGetPropertyValue("isHeaderCompression", out var ihc))
                IsHeaderCompression = ihc?.GetValue<bool>() ?? false;
        }
    }
}
