using System;
using System.Text.Json.Nodes;
using System.Windows.Input;
using Cascade.ViewModels.Operations;
using Cascade.Services;

namespace Cascade.ViewModels.Operations.FFmpeg
{
    /// <summary>
    /// 尺寸与布局子页面ViewModel
    /// </summary>
    public class SizeAndLayoutViewModel : OperationViewModelBase
    {
        public override string SubPageId => "ffmpeg-size-layout";

        #region 输出尺寸
        private int _outputSizeMode;
        public int OutputSizeMode
        {
            get => _outputSizeMode;
            set
            {
                if (SetProperty(ref _outputSizeMode, value))
                {
                    OnPropertyChanged(nameof(IsCustomOutputSize));
                    ValidateOutputDimensions();
                }
            }
        }

        public bool IsCustomOutputSize => OutputSizeMode == 1;

        private string _customWidth = "1920";
        public string CustomWidth
        {
            get => _customWidth;
            set
            {
                if (SetProperty(ref _customWidth, value))
                    ValidateOutputDimensions();
            }
        }

        private string _customHeight = "1080";
        public string CustomHeight
        {
            get => _customHeight;
            set
            {
                if (SetProperty(ref _customHeight, value))
                    ValidateOutputDimensions();
            }
        }
        #endregion

        #region 输出比例
        private int _outputScaleMode;
        public int OutputScaleMode
        {
            get => _outputScaleMode;
            set
            {
                if (SetProperty(ref _outputScaleMode, value))
                    OnPropertyChanged(nameof(IsCustomOutputScale));
            }
        }

        public bool IsCustomOutputScale => OutputScaleMode == 7; // 自定义是第8个选项（索引7）

        private string _customScaleX = "16";
        public string CustomScaleX
        {
            get => _customScaleX;
            set => SetProperty(ref _customScaleX, value);
        }

        private string _customScaleY = "9";
        public string CustomScaleY
        {
            get => _customScaleY;
            set => SetProperty(ref _customScaleY, value);
        }
        #endregion

        #region 变形（Anamorphic）
        private int _anamorphicMode;
        public int AnamorphicMode
        {
            get => _anamorphicMode;
            set
            {
                if (SetProperty(ref _anamorphicMode, value))
                    OnPropertyChanged(nameof(IsCustomAnamorphic));
            }
        }

        public bool IsCustomAnamorphic => AnamorphicMode == 5;

        private string _customAnamorphicX = "1";
        public string CustomAnamorphicX
        {
            get => _customAnamorphicX;
            set => SetProperty(ref _customAnamorphicX, value);
        }

        private string _customAnamorphicY = "1";
        public string CustomAnamorphicY
        {
            get => _customAnamorphicY;
            set => SetProperty(ref _customAnamorphicY, value);
        }
        #endregion

        #region 旋转
        private bool _flipHorizontal;
        public bool FlipHorizontal
        {
            get => _flipHorizontal;
            set => SetProperty(ref _flipHorizontal, value);
        }

        private bool _flipVertical;
        public bool FlipVertical
        {
            get => _flipVertical;
            set => SetProperty(ref _flipVertical, value);
        }

        private double _rotationAngle;
        public double RotationAngle
        {
            get => _rotationAngle;
            set => SetProperty(ref _rotationAngle, value);
        }
        #endregion

        #region 高级旋转
        private double _advancedRotation;
        public double AdvancedRotation
        {
            get => _advancedRotation;
            set
            {
                if (SetProperty(ref _advancedRotation, value))
                {
                    OnPropertyChanged(nameof(IsAdvancedRotationNone));
                    OnPropertyChanged(nameof(PreviewRotationAngle));
                }
            }
        }

        public bool IsAdvancedRotationNone => AdvancedRotation == 0;

        public double PreviewRotationAngle => AdvancedRotation switch
        {
            -1 => 90,  // 全垂直
            1 => 0,    // 全水平
            _ => 0     // 无
        };
        #endregion

        #region 裁切
        private int _cropMode;
        public int CropMode
        {
            get => _cropMode;
            set
            {
                if (SetProperty(ref _cropMode, value))
                {
                    OnPropertyChanged(nameof(IsCustomCrop));
                    ValidateOutputDimensions();
                }
            }
        }

        public bool IsCustomCrop => CropMode == 2;

        private int _cropTop;
        public int CropTop
        {
            get => _cropTop;
            set
            {
                if (SetProperty(ref _cropTop, value))
                    ValidateOutputDimensions();
            }
        }

        private int _cropBottom;
        public int CropBottom
        {
            get => _cropBottom;
            set
            {
                if (SetProperty(ref _cropBottom, value))
                    ValidateOutputDimensions();
            }
        }

        private int _cropLeft;
        public int CropLeft
        {
            get => _cropLeft;
            set
            {
                if (SetProperty(ref _cropLeft, value))
                    ValidateOutputDimensions();
            }
        }

        private int _cropRight;
        public int CropRight
        {
            get => _cropRight;
            set
            {
                if (SetProperty(ref _cropRight, value))
                    ValidateOutputDimensions();
            }
        }

        public ICommand IncreaseCropTopCommand { get; }
        public ICommand DecreaseCropTopCommand { get; }
        public ICommand IncreaseCropBottomCommand { get; }
        public ICommand DecreaseCropBottomCommand { get; }
        public ICommand IncreaseCropLeftCommand { get; }
        public ICommand DecreaseCropLeftCommand { get; }
        public ICommand IncreaseCropRightCommand { get; }
        public ICommand DecreaseCropRightCommand { get; }
        #endregion

        #region 边框
        private int _borderMode;
        public int BorderMode
        {
            get => _borderMode;
            set
            {
                if (SetProperty(ref _borderMode, value))
                {
                    OnPropertyChanged(nameof(IsCustomBorder));
                    ValidateOutputDimensions();
                }
            }
        }

        public bool IsCustomBorder => BorderMode == 1;

        private int _borderColorIndex;
        public int BorderColorIndex
        {
            get => _borderColorIndex;
            set => SetProperty(ref _borderColorIndex, value);
        }

        private int _borderTop;
        public int BorderTop
        {
            get => _borderTop;
            set
            {
                if (SetProperty(ref _borderTop, value))
                    ValidateOutputDimensions();
            }
        }

        private int _borderBottom;
        public int BorderBottom
        {
            get => _borderBottom;
            set
            {
                if (SetProperty(ref _borderBottom, value))
                    ValidateOutputDimensions();
            }
        }

        private int _borderLeft;
        public int BorderLeft
        {
            get => _borderLeft;
            set
            {
                if (SetProperty(ref _borderLeft, value))
                    ValidateOutputDimensions();
            }
        }

        private int _borderRight;
        public int BorderRight
        {
            get => _borderRight;
            set
            {
                if (SetProperty(ref _borderRight, value))
                    ValidateOutputDimensions();
            }
        }

        public ICommand IncreaseBorderTopCommand { get; }
        public ICommand DecreaseBorderTopCommand { get; }
        public ICommand IncreaseBorderBottomCommand { get; }
        public ICommand DecreaseBorderBottomCommand { get; }
        public ICommand IncreaseBorderLeftCommand { get; }
        public ICommand DecreaseBorderLeftCommand { get; }
        public ICommand IncreaseBorderRightCommand { get; }
        public ICommand DecreaseBorderRightCommand { get; }
        #endregion

        public SizeAndLayoutViewModel()
        {
            // 裁切命令
            IncreaseCropTopCommand = new RelayCommand(_ => CropTop++);
            DecreaseCropTopCommand = new RelayCommand(_ => { if (CropTop > 0) CropTop--; });
            IncreaseCropBottomCommand = new RelayCommand(_ => CropBottom++);
            DecreaseCropBottomCommand = new RelayCommand(_ => { if (CropBottom > 0) CropBottom--; });
            IncreaseCropLeftCommand = new RelayCommand(_ => CropLeft++);
            DecreaseCropLeftCommand = new RelayCommand(_ => { if (CropLeft > 0) CropLeft--; });
            IncreaseCropRightCommand = new RelayCommand(_ => CropRight++);
            DecreaseCropRightCommand = new RelayCommand(_ => { if (CropRight > 0) CropRight--; });

            // 边框命令
            IncreaseBorderTopCommand = new RelayCommand(_ => BorderTop++);
            DecreaseBorderTopCommand = new RelayCommand(_ => { if (BorderTop > 0) BorderTop--; });
            IncreaseBorderBottomCommand = new RelayCommand(_ => BorderBottom++);
            DecreaseBorderBottomCommand = new RelayCommand(_ => { if (BorderBottom > 0) BorderBottom--; });
            IncreaseBorderLeftCommand = new RelayCommand(_ => BorderLeft++);
            DecreaseBorderLeftCommand = new RelayCommand(_ => { if (BorderLeft > 0) BorderLeft--; });
            IncreaseBorderRightCommand = new RelayCommand(_ => BorderRight++);
            DecreaseBorderRightCommand = new RelayCommand(_ => { if (BorderRight > 0) BorderRight--; });

            LocalizationService.LanguageChanged += OnLanguageChanged;
            
            // 发布初始值到OperationContext
            PublishInitialValues();
        }

        private void PublishInitialValues()
        {
            PublishData(nameof(OutputSizeMode), OutputSizeMode);
            PublishData(nameof(CustomWidth), CustomWidth);
            PublishData(nameof(CustomHeight), CustomHeight);
            PublishData(nameof(OutputScaleMode), OutputScaleMode);
            PublishData(nameof(CustomScaleX), CustomScaleX);
            PublishData(nameof(CustomScaleY), CustomScaleY);
            PublishData(nameof(AnamorphicMode), AnamorphicMode);
            PublishData(nameof(CustomAnamorphicX), CustomAnamorphicX);
            PublishData(nameof(CustomAnamorphicY), CustomAnamorphicY);
            PublishData(nameof(FlipHorizontal), FlipHorizontal);
            PublishData(nameof(FlipVertical), FlipVertical);
            PublishData(nameof(RotationAngle), RotationAngle);
            PublishData(nameof(AdvancedRotation), AdvancedRotation);
            PublishData(nameof(CropMode), CropMode);
            PublishData(nameof(CropTop), CropTop);
            PublishData(nameof(CropBottom), CropBottom);
            PublishData(nameof(CropLeft), CropLeft);
            PublishData(nameof(CropRight), CropRight);
            PublishData(nameof(BorderMode), BorderMode);
            PublishData(nameof(BorderColorIndex), BorderColorIndex);
            PublishData(nameof(BorderTop), BorderTop);
            PublishData(nameof(BorderBottom), BorderBottom);
            PublishData(nameof(BorderLeft), BorderLeft);
            PublishData(nameof(BorderRight), BorderRight);
        }

        private void OnLanguageChanged(object? sender, string e)
        {
            // 触发相关属性更新，如果 UI 中有直接绑定到本地化字符串的属性
            OnPropertyChanged(nameof(PreviewRotationAngle));
        }

        /// <summary>
        /// 验证输出尺寸是否为偶数（考虑裁切和边框）
        /// </summary>
        private void ValidateOutputDimensions()
        {
            // 如果不是自定义输出尺寸模式，清除警告并返回
            if (OutputSizeMode != 1)
            {
                NotificationService.ClearNotification();
                return;
            }

            // 尝试解析宽度和高度
            if (!int.TryParse(CustomWidth, out int width) || !int.TryParse(CustomHeight, out int height))
            {
                // 无法解析时也清除警告
                NotificationService.ClearNotification();
                return;
            }

            // 计算最终尺寸（考虑裁切和边框）
            int finalWidth = width;
            int finalHeight = height;

            // 应用裁切（如果启用自定义裁切）
            if (CropMode == 2)
            {
                finalWidth = finalWidth - CropLeft - CropRight;
                finalHeight = finalHeight - CropTop - CropBottom;
            }

            // 应用边框（如果启用自定义边框）
            if (BorderMode == 1)
            {
                finalWidth = finalWidth + BorderLeft + BorderRight;
                finalHeight = finalHeight + BorderTop + BorderBottom;
            }

            // 检查最终尺寸是否为偶数
            bool isWidthEven = finalWidth % 2 == 0;
            bool isHeightEven = finalHeight % 2 == 0;

            if (!isWidthEven || !isHeightEven)
            {
                // 发送警告
                NotificationService.SendWarning("Warning_OutputDimensionNotEven");
            }
            else
            {
                // 清除警告
                NotificationService.ClearNotification();
            }
        }

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["outputSizeMode"] = OutputSizeMode,
                ["customWidth"] = CustomWidth,
                ["customHeight"] = CustomHeight,
                ["outputScaleMode"] = OutputScaleMode,
                ["customScaleX"] = CustomScaleX,
                ["customScaleY"] = CustomScaleY,
                ["anamorphicMode"] = AnamorphicMode,
                ["customAnamorphicX"] = CustomAnamorphicX,
                ["customAnamorphicY"] = CustomAnamorphicY,
                ["flipHorizontal"] = FlipHorizontal,
                ["flipVertical"] = FlipVertical,
                ["rotationAngle"] = RotationAngle,
                ["advancedRotation"] = AdvancedRotation,
                ["cropMode"] = CropMode,
                ["cropTop"] = CropTop,
                ["cropBottom"] = CropBottom,
                ["cropLeft"] = CropLeft,
                ["cropRight"] = CropRight,
                ["borderMode"] = BorderMode,
                ["borderColorIndex"] = BorderColorIndex,
                ["borderTop"] = BorderTop,
                ["borderBottom"] = BorderBottom,
                ["borderLeft"] = BorderLeft,
                ["borderRight"] = BorderRight
            };
        }

        public override void Deserialize(JsonObject data)
        {
            if (data.TryGetPropertyValue("outputSizeMode", out var osm)) OutputSizeMode = osm?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("customWidth", out var cw)) CustomWidth = cw?.GetValue<string>() ?? "1920";
            if (data.TryGetPropertyValue("customHeight", out var ch)) CustomHeight = ch?.GetValue<string>() ?? "1080";
            if (data.TryGetPropertyValue("outputScaleMode", out var oscm)) OutputScaleMode = oscm?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("customScaleX", out var csx)) CustomScaleX = csx?.GetValue<string>() ?? "16";
            if (data.TryGetPropertyValue("customScaleY", out var csy)) CustomScaleY = csy?.GetValue<string>() ?? "9";
            if (data.TryGetPropertyValue("anamorphicMode", out var am)) AnamorphicMode = am?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("customAnamorphicX", out var cax)) CustomAnamorphicX = cax?.GetValue<string>() ?? "1";
            if (data.TryGetPropertyValue("customAnamorphicY", out var cay)) CustomAnamorphicY = cay?.GetValue<string>() ?? "1";
            if (data.TryGetPropertyValue("flipHorizontal", out var fh)) FlipHorizontal = fh?.GetValue<bool>() ?? false;
            if (data.TryGetPropertyValue("flipVertical", out var fv)) FlipVertical = fv?.GetValue<bool>() ?? false;
            if (data.TryGetPropertyValue("rotationAngle", out var ra)) RotationAngle = ra?.GetValue<double>() ?? 0;
            if (data.TryGetPropertyValue("advancedRotation", out var ar)) AdvancedRotation = ar?.GetValue<double>() ?? 0;
            if (data.TryGetPropertyValue("cropMode", out var cm)) CropMode = cm?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("cropTop", out var ct)) CropTop = ct?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("cropBottom", out var cb)) CropBottom = cb?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("cropLeft", out var cl)) CropLeft = cl?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("cropRight", out var cr)) CropRight = cr?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("borderMode", out var bm)) BorderMode = bm?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("borderColorIndex", out var bci)) BorderColorIndex = bci?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("borderTop", out var bt)) BorderTop = bt?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("borderBottom", out var bb)) BorderBottom = bb?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("borderLeft", out var bl)) BorderLeft = bl?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("borderRight", out var br)) BorderRight = br?.GetValue<int>() ?? 0;
        }
    }
}
