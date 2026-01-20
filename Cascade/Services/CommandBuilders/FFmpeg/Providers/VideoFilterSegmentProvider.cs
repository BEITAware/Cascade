using System;
using System.Collections.Generic;
using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders.FFmpeg.Providers
{
    /// <summary>
    /// 视频滤镜命令片段提供者
    /// </summary>
    public class VideoFilterSegmentProvider : ICommandSegmentProvider
    {
        public CommandSegmentType SegmentType => CommandSegmentType.VideoFilters;
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
                var filters = new List<string>();
                
                // 1. 尺寸调整 (Scale)
                var outputSizeMode = context.GetData<int>("ffmpeg-size-layout", "OutputSizeMode");
                if (outputSizeMode == 1) // 自定义尺寸
                {
                    var width = context.GetData<string>("ffmpeg-size-layout", "CustomWidth");
                    var height = context.GetData<string>("ffmpeg-size-layout", "CustomHeight");
                    
                    if (!string.IsNullOrEmpty(width) && !string.IsNullOrEmpty(height))
                    {
                        filters.Add($"scale={width}:{height}");
                    }
                }
                
                // 2. 输出比例 (Output Scale / Display Aspect Ratio)
                var outputScaleMode = context.GetData<int>("ffmpeg-size-layout", "OutputScaleMode");
                if (outputScaleMode > 0) // 非自动
                {
                    string? darRatio = outputScaleMode switch
                    {
                        1 => "1/1",      // 1:1 (正方形)
                        2 => "4/3",      // 4:3 (标准)
                        3 => "16/9",     // 16:9 (宽屏)
                        4 => "21/9",     // 21:9 (超宽)
                        5 => "2.35/1",   // 2.35:1 (电影)
                        6 => "2.39/1",   // 2.39:1 (电影宽屏)
                        7 => GetCustomOutputScale(context), // 自定义
                        _ => null
                    };
                    
                    if (!string.IsNullOrEmpty(darRatio))
                    {
                        filters.Add($"setdar={darRatio}");
                    }
                }
                
                // 3. 变形（Anamorphic / SAR - Sample Aspect Ratio）
                var anamorphicMode = context.GetData<int>("ffmpeg-size-layout", "AnamorphicMode");
                if (anamorphicMode > 0) // 非正方形
                {
                    string sarRatio = anamorphicMode switch
                    {
                        1 => "4/3",      // 4:3
                        2 => "16/9",     // 16:9
                        3 => "2.35/1",   // 2.35:1
                        4 => "2.39/1",   // 2.39:1
                        5 => GetCustomAnamorphicRatio(context), // 自定义
                        _ => "1/1"
                    };
                    
                    if (!string.IsNullOrEmpty(sarRatio))
                    {
                        filters.Add($"setsar={sarRatio}");
                    }
                }
                
                // 4. 裁切 (Crop)
                var cropMode = context.GetData<int>("ffmpeg-size-layout", "CropMode");
                if (cropMode == 2) // 自定义裁切
                {
                    var cropTop = context.GetData<int>("ffmpeg-size-layout", "CropTop");
                    var cropBottom = context.GetData<int>("ffmpeg-size-layout", "CropBottom");
                    var cropLeft = context.GetData<int>("ffmpeg-size-layout", "CropLeft");
                    var cropRight = context.GetData<int>("ffmpeg-size-layout", "CropRight");
                    
                    // 只有当裁切值不全为0时才添加裁切滤镜
                    if (cropTop > 0 || cropBottom > 0 || cropLeft > 0 || cropRight > 0)
                    {
                        filters.Add($"crop=iw-{cropLeft}-{cropRight}:ih-{cropTop}-{cropBottom}:{cropLeft}:{cropTop}");
                    }
                }
                
                // 5. 翻转 (Flip)
                var flipHorizontal = context.GetData<bool>("ffmpeg-size-layout", "FlipHorizontal");
                var flipVertical = context.GetData<bool>("ffmpeg-size-layout", "FlipVertical");
                
                if (flipHorizontal)
                {
                    filters.Add("hflip");
                }
                
                if (flipVertical)
                {
                    filters.Add("vflip");
                }
                
                // 6. 旋转 (Rotate)
                var rotationAngle = context.GetData<double>("ffmpeg-size-layout", "RotationAngle");
                if (Math.Abs(rotationAngle) > 0.001) // 避免浮点数精度问题
                {
                    // 将角度转换为弧度
                    var radians = rotationAngle * Math.PI / 180.0;
                    filters.Add($"rotate={radians:F6}");
                }
                
                // 7. 边框 (Pad/Border)
                var borderMode = context.GetData<int>("ffmpeg-size-layout", "BorderMode");
                if (borderMode == 1) // 自定义边框
                {
                    var borderTop = context.GetData<int>("ffmpeg-size-layout", "BorderTop");
                    var borderBottom = context.GetData<int>("ffmpeg-size-layout", "BorderBottom");
                    var borderLeft = context.GetData<int>("ffmpeg-size-layout", "BorderLeft");
                    var borderRight = context.GetData<int>("ffmpeg-size-layout", "BorderRight");
                    var borderColorIndex = context.GetData<int>("ffmpeg-size-layout", "BorderColorIndex");
                    
                    // 只有当边框值不全为0时才添加边框滤镜
                    if (borderTop > 0 || borderBottom > 0 || borderLeft > 0 || borderRight > 0)
                    {
                        var color = GetBorderColor(borderColorIndex);
                        var totalWidth = $"iw+{borderLeft}+{borderRight}";
                        var totalHeight = $"ih+{borderTop}+{borderBottom}";
                        filters.Add($"pad={totalWidth}:{totalHeight}:{borderLeft}:{borderTop}:{color}");
                    }
                }
                
                // 组装滤镜链
                if (filters.Count > 0)
                {
                    segment.Parameters = $"-vf \"{string.Join(",", filters)}\"";
                }
                else
                {
                    segment.Parameters = string.Empty;
                }
                
                segment.IsValid = true;
            }
            catch (Exception ex)
            {
                segment.IsValid = false;
                segment.ValidationMessage = $"Error generating video filter segment: {ex.Message}";
            }
            
            return segment;
        }
        
        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();
            
            try
            {
                // 验证自定义尺寸
                var outputSizeMode = context.GetData<int>("ffmpeg-size-layout", "OutputSizeMode");
                if (outputSizeMode == 1)
                {
                    var width = context.GetData<string>("ffmpeg-size-layout", "CustomWidth");
                    var height = context.GetData<string>("ffmpeg-size-layout", "CustomHeight");
                    
                    if (string.IsNullOrWhiteSpace(width) || string.IsNullOrWhiteSpace(height))
                    {
                        result.AddError("Custom output size requires both width and height");
                    }
                    else
                    {
                        if (!int.TryParse(width, out var w) || w <= 0)
                        {
                            result.AddError($"Invalid width value: {width}");
                        }
                        
                        if (!int.TryParse(height, out var h) || h <= 0)
                        {
                            result.AddError($"Invalid height value: {height}");
                        }
                    }
                }
                
                // 验证自定义输出比例
                var outputScaleMode = context.GetData<int>("ffmpeg-size-layout", "OutputScaleMode");
                if (outputScaleMode == 7) // 自定义
                {
                    var x = context.GetData<string>("ffmpeg-size-layout", "CustomScaleX");
                    var y = context.GetData<string>("ffmpeg-size-layout", "CustomScaleY");
                    
                    if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y))
                    {
                        result.AddError("Custom output scale requires both X and Y values");
                    }
                    else
                    {
                        if (!double.TryParse(x, out var xVal) || xVal <= 0)
                        {
                            result.AddError($"Invalid output scale X value: {x}");
                        }
                        
                        if (!double.TryParse(y, out var yVal) || yVal <= 0)
                        {
                            result.AddError($"Invalid output scale Y value: {y}");
                        }
                    }
                }
                
                // 验证自定义Anamorphic比例
                var anamorphicMode = context.GetData<int>("ffmpeg-size-layout", "AnamorphicMode");
                if (anamorphicMode == 5) // 自定义
                {
                    var x = context.GetData<string>("ffmpeg-size-layout", "CustomAnamorphicX");
                    var y = context.GetData<string>("ffmpeg-size-layout", "CustomAnamorphicY");
                    
                    if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y))
                    {
                        result.AddError("Custom anamorphic ratio requires both X and Y values");
                    }
                    else
                    {
                        if (!double.TryParse(x, out var xVal) || xVal <= 0)
                        {
                            result.AddError($"Invalid anamorphic X value: {x}");
                        }
                        
                        if (!double.TryParse(y, out var yVal) || yVal <= 0)
                        {
                            result.AddError($"Invalid anamorphic Y value: {y}");
                        }
                    }
                }
                
                // 验证裁切值
                var cropMode = context.GetData<int>("ffmpeg-size-layout", "CropMode");
                if (cropMode == 2)
                {
                    var cropTop = context.GetData<int>("ffmpeg-size-layout", "CropTop");
                    var cropBottom = context.GetData<int>("ffmpeg-size-layout", "CropBottom");
                    var cropLeft = context.GetData<int>("ffmpeg-size-layout", "CropLeft");
                    var cropRight = context.GetData<int>("ffmpeg-size-layout", "CropRight");
                    
                    if (cropTop < 0 || cropBottom < 0 || cropLeft < 0 || cropRight < 0)
                    {
                        result.AddError("Crop values cannot be negative");
                    }
                }
                
                // 验证边框值
                var borderMode = context.GetData<int>("ffmpeg-size-layout", "BorderMode");
                if (borderMode == 1)
                {
                    var borderTop = context.GetData<int>("ffmpeg-size-layout", "BorderTop");
                    var borderBottom = context.GetData<int>("ffmpeg-size-layout", "BorderBottom");
                    var borderLeft = context.GetData<int>("ffmpeg-size-layout", "BorderLeft");
                    var borderRight = context.GetData<int>("ffmpeg-size-layout", "BorderRight");
                    
                    if (borderTop < 0 || borderBottom < 0 || borderLeft < 0 || borderRight < 0)
                    {
                        result.AddError("Border values cannot be negative");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Validation error: {ex.Message}");
            }
            
            return result;
        }
        
        private string GetBorderColor(int colorIndex)
        {
            return colorIndex switch
            {
                0 => "black",
                1 => "white",
                2 => "gray",
                3 => "red",
                4 => "green",
                5 => "blue",
                _ => "black"
            };
        }
        
        private string GetCustomOutputScale(OperationContext context)
        {
            var x = context.GetData<string>("ffmpeg-size-layout", "CustomScaleX", "16");
            var y = context.GetData<string>("ffmpeg-size-layout", "CustomScaleY", "9");
            
            // 验证输入
            if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y))
            {
                return "16/9";
            }
            
            // 尝试解析为数字以验证
            if (double.TryParse(x, out var xVal) && double.TryParse(y, out var yVal) && 
                xVal > 0 && yVal > 0)
            {
                return $"{x}/{y}";
            }
            
            return "16/9";
        }
        
        private string GetCustomAnamorphicRatio(OperationContext context)
        {
            var x = context.GetData<string>("ffmpeg-size-layout", "CustomAnamorphicX", "1");
            var y = context.GetData<string>("ffmpeg-size-layout", "CustomAnamorphicY", "1");
            
            // 验证输入
            if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y))
            {
                return "1/1";
            }
            
            // 尝试解析为数字以验证
            if (double.TryParse(x, out var xVal) && double.TryParse(y, out var yVal) && 
                xVal > 0 && yVal > 0)
            {
                return $"{x}/{y}";
            }
            
            return "1/1";
        }
    }
}
