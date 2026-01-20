using System;
using System.Collections.Generic;
using System.Linq;

namespace Cascade.Services.FFmpeg
{
    /// <summary>
    /// 编码器映射服务
    /// 将UI显示名称映射到FFmpeg编码器名称，支持本地化
    /// </summary>
    public class EncoderMappingService
    {
        private readonly Dictionary<string, string> _encoderMap;
        
        public EncoderMappingService()
        {
            _encoderMap = InitializeEncoderMap();
        }
        
        /// <summary>
        /// 初始化编码器映射表
        /// 映射UI显示名称（英文）到FFmpeg编码器名称
        /// </summary>
        /// <returns>编码器映射字典</returns>
        private Dictionary<string, string> InitializeEncoderMap()
        {
            return new Dictionary<string, string>
            {
                // H.264 编码器
                ["x264"] = "libx264",
                ["NVENC H.264"] = "h264_nvenc",
                ["QSV H.264"] = "h264_qsv",
                ["AMF H.264"] = "h264_amf",
                ["VideoToolbox H.264"] = "h264_videotoolbox",
                
                // H.265 编码器
                ["x265"] = "libx265",
                ["NVENC HEVC"] = "hevc_nvenc",
                ["QSV HEVC"] = "hevc_qsv",
                ["AMF HEVC"] = "hevc_amf",
                ["VideoToolbox HEVC"] = "hevc_videotoolbox",
                
                // H.266 编码器
                ["vvenc"] = "libvvenc",
                
                // AV1 编码器
                ["libaom-av1"] = "libaom-av1",
                ["libsvtav1"] = "libsvtav1",
                ["librav1e"] = "librav1e",
                ["NVENC AV1"] = "av1_nvenc",
                ["QSV AV1"] = "av1_qsv",
                ["AMF AV1"] = "av1_amf",
                
                // VP9 编码器
                ["libvpx-vp9"] = "libvpx-vp9",
                
                // ProRes 编码器
                ["prores_ks"] = "prores_ks",
                ["prores_aw"] = "prores_aw",
                
                // 无压缩编码器
                ["rawvideo"] = "rawvideo",
                ["ffv1"] = "ffv1",
                ["huffyuv"] = "huffyuv",
                
                // 复制流
                ["copy"] = "copy"
            };
        }
        
        /// <summary>
        /// 获取FFmpeg编码器名称
        /// </summary>
        /// <param name="displayName">UI显示名称（可以是本地化的或英文的）</param>
        /// <returns>FFmpeg编码器名称，如果未找到返回null</returns>
        public string? GetFFmpegEncoderName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                System.Diagnostics.Debug.WriteLine("EncoderMappingService: displayName is null or empty");
                return null;
            }
            
            // 首先尝试直接匹配（英文名称）
            if (_encoderMap.TryGetValue(displayName, out var ffmpegName))
            {
                return ffmpegName;
            }
            
            // 如果直接匹配失败，尝试通过本地化键匹配
            // 遍历所有编码器，检查其本地化名称是否匹配
            foreach (var kvp in _encoderMap)
            {
                var localizedName = GetLocalizedDisplayName(kvp.Value);
                if (localizedName == displayName)
                {
                    return kvp.Value;
                }
            }
            
            // 未找到映射，记录警告
            System.Diagnostics.Debug.WriteLine($"EncoderMappingService: Unknown encoder display name: {displayName}");
            return null;
        }
        
        /// <summary>
        /// 获取本地化显示名称
        /// </summary>
        /// <param name="ffmpegName">FFmpeg编码器名称</param>
        /// <returns>本地化显示名称</returns>
        public string GetLocalizedDisplayName(string ffmpegName)
        {
            if (string.IsNullOrEmpty(ffmpegName))
                return string.Empty;
            
            // 查找反向映射（FFmpeg名称 -> UI显示名称）
            var displayName = _encoderMap.FirstOrDefault(kvp => kvp.Value == ffmpegName).Key;
            if (displayName == null)
            {
                // 如果找不到映射，返回原始FFmpeg名称
                return ffmpegName;
            }
            
            // 尝试获取本地化版本
            // 将显示名称转换为资源键格式
            var resourceKey = GetResourceKeyForEncoder(displayName);
            var localizedName = LocalizationService.GetString(resourceKey);
            
            // 如果本地化键不存在，LocalizationService会返回键本身
            // 在这种情况下，返回英文显示名称
            return localizedName != resourceKey ? localizedName : displayName;
        }
        
        /// <summary>
        /// 获取所有支持的编码器
        /// </summary>
        /// <returns>编码器映射字典（只读）</returns>
        public IReadOnlyDictionary<string, string> GetAllEncoders()
        {
            return _encoderMap;
        }
        
        /// <summary>
        /// 获取所有本地化的编码器显示名称
        /// </summary>
        /// <returns>本地化显示名称列表</returns>
        public IReadOnlyList<string> GetAllLocalizedDisplayNames()
        {
            return _encoderMap.Values
                .Select(GetLocalizedDisplayName)
                .ToList();
        }
        
        /// <summary>
        /// 检查编码器是否被支持
        /// </summary>
        /// <param name="displayName">UI显示名称</param>
        /// <returns>是否支持</returns>
        public bool IsEncoderSupported(string displayName)
        {
            return GetFFmpegEncoderName(displayName) != null;
        }
        
        /// <summary>
        /// 添加或更新编码器映射
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="ffmpegName">FFmpeg编码器名称</param>
        public void AddOrUpdateEncoder(string displayName, string ffmpegName)
        {
            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(ffmpegName))
                return;
            
            _encoderMap[displayName] = ffmpegName;
        }
        
        /// <summary>
        /// 移除编码器映射
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveEncoder(string displayName)
        {
            return _encoderMap.Remove(displayName);
        }
        
        /// <summary>
        /// 将编码器显示名称转换为资源键
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <returns>资源键</returns>
        private string GetResourceKeyForEncoder(string displayName)
        {
            // 将显示名称转换为资源键格式
            // 例如: "NVENC H.264" -> "VideoEncoder_NVENC_H264"
            var key = displayName
                .Replace(" ", "_")
                .Replace(".", "")
                .Replace("-", "");
            
            return $"VideoEncoder_{key}";
        }
    }
}