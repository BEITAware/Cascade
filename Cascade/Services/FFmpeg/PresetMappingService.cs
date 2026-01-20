using System;
using System.Collections.Generic;
using Cascade.Services;

namespace Cascade.Services.FFmpeg
{
    /// <summary>
    /// 编码器类型枚举
    /// </summary>
    public enum EncoderType
    {
        X264,
        X265,
        VVC,
        NVENC,
        QSV,
        AV1,
        VP9,
        ProRes
    }
    
    /// <summary>
    /// 预设范围信息
    /// </summary>
    public class PresetRange
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public int MinValue { get; set; }
        
        /// <summary>
        /// 最大值
        /// </summary>
        public int MaxValue { get; set; }
        
        /// <summary>
        /// 默认值
        /// </summary>
        public int DefaultValue { get; set; }
        
        /// <summary>
        /// 最小值标签
        /// </summary>
        public string MinLabel { get; set; } = string.Empty;
        
        /// <summary>
        /// 最大值标签
        /// </summary>
        public string MaxLabel { get; set; } = string.Empty;
    }

    /// <summary>
    /// 预设映射服务
    /// </summary>
    public class PresetMappingService
    {
        private readonly Dictionary<EncoderType, string[]> _presetMappings;
        private readonly Dictionary<EncoderType, PresetRange> _presetRanges;
        
        public PresetMappingService()
        {
            _presetMappings = InitializePresetMappings();
            _presetRanges = InitializePresetRanges();
        }
        
        /// <summary>
        /// 初始化预设映射
        /// </summary>
        /// <returns>预设映射字典</returns>
        private Dictionary<EncoderType, string[]> InitializePresetMappings()
        {
            return new Dictionary<EncoderType, string[]>
            {
                [EncoderType.X264] = new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "placebo" },
                [EncoderType.X265] = new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "placebo" },
                [EncoderType.VVC] = new[] { "faster", "fast", "medium", "slow", "slower" },
                [EncoderType.NVENC] = new[] { "p1", "p2", "p3", "p4", "p5", "p6", "p7" },
                [EncoderType.QSV] = new[] { "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" },
                [EncoderType.AV1] = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" },
                [EncoderType.VP9] = new[] { "0", "1", "2", "3", "4" },
                [EncoderType.ProRes] = new[] { "proxy", "lt", "standard", "hq", "4444", "4444xq" }
            };
        }
        
        /// <summary>
        /// 初始化预设范围
        /// </summary>
        /// <returns>预设范围字典</returns>
        private Dictionary<EncoderType, PresetRange> InitializePresetRanges()
        {
            return new Dictionary<EncoderType, PresetRange>
            {
                [EncoderType.X264] = new PresetRange { MinValue = 0, MaxValue = 9, DefaultValue = 5, MinLabel = "Fast", MaxLabel = "Slow" },
                [EncoderType.X265] = new PresetRange { MinValue = 0, MaxValue = 9, DefaultValue = 5, MinLabel = "Fast", MaxLabel = "Slow" },
                [EncoderType.VVC] = new PresetRange { MinValue = 0, MaxValue = 4, DefaultValue = 2, MinLabel = "Fast", MaxLabel = "Slow" },
                [EncoderType.NVENC] = new PresetRange { MinValue = 0, MaxValue = 6, DefaultValue = 3, MinLabel = "Fast", MaxLabel = "Slow" },
                [EncoderType.QSV] = new PresetRange { MinValue = 0, MaxValue = 6, DefaultValue = 3, MinLabel = "Fast", MaxLabel = "Slow" },
                [EncoderType.AV1] = new PresetRange { MinValue = 0, MaxValue = 12, DefaultValue = 6, MinLabel = "Fast", MaxLabel = "Slow" },
                [EncoderType.VP9] = new PresetRange { MinValue = 0, MaxValue = 4, DefaultValue = 2, MinLabel = "Fast", MaxLabel = "Slow" },
                [EncoderType.ProRes] = new PresetRange { MinValue = 0, MaxValue = 5, DefaultValue = 2, MinLabel = "Low Quality", MaxLabel = "High Quality" }
            };
        }
        
        /// <summary>
        /// 获取预设字符串
        /// </summary>
        /// <param name="encoderType">编码器类型</param>
        /// <param name="presetValue">预设值</param>
        /// <returns>FFmpeg预设字符串</returns>
        public string GetPresetString(EncoderType encoderType, int presetValue)
        {
            if (!_presetMappings.TryGetValue(encoderType, out var presets))
                return "medium"; // 默认预设
                
            if (presetValue < 0 || presetValue >= presets.Length)
                return presets[presets.Length / 2]; // 返回中间值
                
            return presets[presetValue];
        }
        
        /// <summary>
        /// 获取本地化预设标签
        /// </summary>
        /// <param name="encoderType">编码器类型</param>
        /// <param name="presetValue">预设值</param>
        /// <returns>本地化预设标签</returns>
        public string GetLocalizedPresetLabel(EncoderType encoderType, int presetValue)
        {
            var presetString = GetPresetString(encoderType, presetValue);
            var resourceKey = $"VideoEncoder_Preset_{presetString}";
            var localizedLabel = LocalizationService.GetString(resourceKey);
            
            // 如果没有找到本地化字符串，返回原始预设字符串
            return localizedLabel != resourceKey ? localizedLabel : presetString;
        }
        
        /// <summary>
        /// 获取预设范围
        /// </summary>
        /// <param name="encoderType">编码器类型</param>
        /// <returns>预设范围信息</returns>
        public PresetRange GetPresetRange(EncoderType encoderType)
        {
            if (_presetRanges.TryGetValue(encoderType, out var range))
            {
                // 返回本地化的范围信息
                return new PresetRange
                {
                    MinValue = range.MinValue,
                    MaxValue = range.MaxValue,
                    DefaultValue = range.DefaultValue,
                    MinLabel = LocalizationService.GetString($"VideoEncoder_Preset_Min_{encoderType}"),
                    MaxLabel = LocalizationService.GetString($"VideoEncoder_Preset_Max_{encoderType}")
                };
            }
            
            // 默认范围
            return new PresetRange
            {
                MinValue = 0,
                MaxValue = 9,
                DefaultValue = 5,
                MinLabel = LocalizationService.GetString("VideoEncoder_Preset_Fast"),
                MaxLabel = LocalizationService.GetString("VideoEncoder_Preset_Slow")
            };
        }
        
        /// <summary>
        /// 获取编码器类型对应的预设数量
        /// </summary>
        /// <param name="encoderType">编码器类型</param>
        /// <returns>预设数量</returns>
        public int GetPresetCount(EncoderType encoderType)
        {
            return _presetMappings.TryGetValue(encoderType, out var presets) ? presets.Length : 10;
        }
        
        /// <summary>
        /// 检查预设值是否有效
        /// </summary>
        /// <param name="encoderType">编码器类型</param>
        /// <param name="presetValue">预设值</param>
        /// <returns>是否有效</returns>
        public bool IsValidPresetValue(EncoderType encoderType, int presetValue)
        {
            var range = GetPresetRange(encoderType);
            return presetValue >= range.MinValue && presetValue <= range.MaxValue;
        }
    }
}