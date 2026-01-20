using System;
using System.Collections.Generic;
using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders.FFmpeg.Providers
{
    /// <summary>
    /// 输出选项命令片段提供者
    /// </summary>
    public class OutputOptionsSegmentProvider : ICommandSegmentProvider
    {
        public CommandSegmentType SegmentType => CommandSegmentType.OutputOptions;
        public int Priority => 0;
        
        public CommandSegment GenerateSegment(OperationContext context)
        {
            var segment = new CommandSegment
            {
                Type = SegmentType,
                Priority = Priority
            };
            
            try
            {
                var options = new List<string>();
                
                // 获取容器选择
                var containerIndex = context.GetData<int>("ffmpeg-muxing-delivery", "SelectedContainerIndex");
                
                // 容器格式参数
                var formatString = GetContainerFormat(containerIndex);
                if (!string.IsNullOrEmpty(formatString))
                {
                    options.Add($"-f {formatString}");
                }
                
                // MP4 网络优化
                if (containerIndex == 1) // MP4
                {
                    var isWebOptimized = context.GetData<bool>("ffmpeg-muxing-delivery", "IsWebOptimized");
                    if (isWebOptimized)
                    {
                        options.Add("-movflags +faststart");
                    }
                }
                
                // MKV 头部压缩
                if (containerIndex == 0) // MKV
                {
                    var isHeaderCompression = context.GetData<bool>("ffmpeg-muxing-delivery", "IsHeaderCompression");
                    if (isHeaderCompression)
                    {
                        options.Add("-compression_level 9");
                    }
                }
                
                segment.Parameters = string.Join(" ", options);
                segment.IsValid = true;
            }
            catch (Exception ex)
            {
                segment.IsValid = false;
                segment.ValidationMessage = $"Error generating output options segment: {ex.Message}";
            }
            
            return segment;
        }
        
        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();
            
            try
            {
                var containerIndex = context.GetData<int>("ffmpeg-muxing-delivery", "SelectedContainerIndex");
                
                // 验证容器索引在有效范围内
                if (containerIndex < 0 || containerIndex > 4)
                {
                    result.AddError($"Invalid container index: {containerIndex}");
                }
                
                // 验证网络优化选项仅在MP4时启用
                var isWebOptimized = context.GetData<bool>("ffmpeg-muxing-delivery", "IsWebOptimized");
                if (isWebOptimized && containerIndex != 1)
                {
                    result.AddWarning("Web optimization is only applicable to MP4 container");
                }
                
                // 验证头部压缩选项仅在MKV时启用
                var isHeaderCompression = context.GetData<bool>("ffmpeg-muxing-delivery", "IsHeaderCompression");
                if (isHeaderCompression && containerIndex != 0)
                {
                    result.AddWarning("Header compression is only applicable to MKV container");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Validation error: {ex.Message}");
            }
            
            return result;
        }
        
        private string GetContainerFormat(int containerIndex)
        {
            return containerIndex switch
            {
                0 => "matroska",  // MKV
                1 => "mp4",        // MP4
                2 => "mov",        // QuickTime
                3 => "mpegts",     // TS
                4 => "flv",        // FLV
                _ => ""
            };
        }
    }
}
