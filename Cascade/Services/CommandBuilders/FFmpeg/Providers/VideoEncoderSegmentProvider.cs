using System;
using System.Collections.Generic;
using Cascade.Services.FFmpeg;
using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders.FFmpeg.Providers
{
    /// <summary>
    /// 视频编码器命令片段提供者
    /// </summary>
    public class VideoEncoderSegmentProvider : ICommandSegmentProvider
    {
        private readonly EncoderMappingService _encoderMapping;
        private readonly PresetMappingService _presetMapping;
        
        public CommandSegmentType SegmentType => CommandSegmentType.VideoCodec;
        public int Priority => 0;
        
        public VideoEncoderSegmentProvider()
        {
            _encoderMapping = new EncoderMappingService();
            _presetMapping = new PresetMappingService();
        }
        
        public CommandSegment GenerateSegment(OperationContext context)
        {
            var segment = new CommandSegment
            {
                Type = SegmentType,
                Priority = Priority
            };
            
            try
            {
                var parameters = new List<string>();
                
                // 获取编码器选择
                var codecIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedCodecIndex");
                var encoderIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedEncoderIndex");
                
                // 复制流模式
                if (codecIndex == 0)
                {
                    parameters.Add("-c:v copy");
                    segment.Parameters = string.Join(" ", parameters);
                    segment.IsValid = true;
                    return segment;
                }
                
                // 获取编码器名称
                var encoderDisplayName = GetEncoderDisplayName(codecIndex, encoderIndex);
                var ffmpegEncoderName = _encoderMapping.GetFFmpegEncoderName(encoderDisplayName);
                
                if (string.IsNullOrEmpty(ffmpegEncoderName))
                {
                    segment.IsValid = false;
                    segment.ValidationMessage = $"Unknown encoder: {encoderDisplayName}";
                    return segment;
                }
                
                parameters.Add($"-c:v {ffmpegEncoderName}");
                
                // 预设 (Preset)
                if (codecIndex != 7) // 非无压缩
                {
                    var presetValue = context.GetData<int>("ffmpeg-video-encoder-strategy", "PresetValue");
                    var encoderType = GetEncoderType(codecIndex, encoderIndex);
                    var presetString = _presetMapping.GetPresetString(encoderType, presetValue);
                    
                    if (!string.IsNullOrEmpty(presetString))
                    {
                        parameters.Add($"-preset {presetString}");
                    }
                }
                
                // 码率控制模式
                var rateModeIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedRateModeIndex");
                
                if (rateModeIndex == 0) // CRF模式
                {
                    var crfValue = context.GetData<int>("ffmpeg-video-encoder-strategy", "CrfValue");
                    parameters.Add($"-crf {crfValue}");
                }
                else if (rateModeIndex >= 1 && rateModeIndex <= 3) // 比特率模式
                {
                    var targetBitrate = context.GetData<int>("ffmpeg-video-encoder-strategy", "TargetBitrate");
                    parameters.Add($"-b:v {targetBitrate}k");
                }
                else if (rateModeIndex == 4) // CQP模式
                {
                    var qpI = context.GetData<int>("ffmpeg-video-encoder-strategy", "QpI");
                    var qpP = context.GetData<int>("ffmpeg-video-encoder-strategy", "QpP");
                    var qpB = context.GetData<int>("ffmpeg-video-encoder-strategy", "QpB");
                    parameters.Add($"-qp {qpI}:{qpP}:{qpB}");
                }
                
                // 微调 (Tune) - 仅x264/x265
                if (IsX264OrX265(codecIndex, encoderIndex))
                {
                    var tuneIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedTuneIndex");
                    if (tuneIndex > 0) // 0 = 无
                    {
                        var tuneOptions = new[] { "", "film", "animation", "grain", "stillimage", "fastdecode", "zerolatency" };
                        if (tuneIndex < tuneOptions.Length)
                        {
                            parameters.Add($"-tune {tuneOptions[tuneIndex]}");
                        }
                    }
                }
                
                // 像素格式 (Pixel Format) - 需要先获取，因为profile依赖它
                var pixelFormatIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedPixelFormatIndex");
                
                // 配置文件 (Profile) - 注意：libvvenc不支持profile参数
                if (codecIndex >= 1 && codecIndex <= 2) // H.264/H.265 only
                {
                    var profileValue = context.GetData<int>("ffmpeg-video-encoder-strategy", "ProfileValue");
                    var profileString = GetProfileString(codecIndex, profileValue, pixelFormatIndex);
                    if (!string.IsNullOrEmpty(profileString))
                    {
                        parameters.Add($"-profile:v {profileString}");
                    }
                }
                
                // 等级 (Level)
                if (codecIndex >= 1 && codecIndex <= 3) // H.264/H.265/H.266
                {
                    var levelValue = context.GetData<int>("ffmpeg-video-encoder-strategy", "LevelValue");
                    if (levelValue > 0) // 0 = 自动
                    {
                        var levelString = GetLevelString(codecIndex, levelValue);
                        if (!string.IsNullOrEmpty(levelString))
                        {
                            parameters.Add($"-level {levelString}");
                        }
                    }
                }
                
                // 输出像素格式参数
                if (pixelFormatIndex > 0) // 0 = 自动
                {
                    var pixelFormat = GetPixelFormat(pixelFormatIndex);
                    if (!string.IsNullOrEmpty(pixelFormat))
                    {
                        parameters.Add($"-pix_fmt {pixelFormat}");
                    }
                }
                
                segment.Parameters = string.Join(" ", parameters);
                segment.IsValid = true;
            }
            catch (Exception ex)
            {
                segment.IsValid = false;
                segment.ValidationMessage = $"Error generating video encoder segment: {ex.Message}";
            }
            
            return segment;
        }
        
        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();
            
            try
            {
                var codecIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedCodecIndex");
                
                // 复制流模式不需要验证
                if (codecIndex == 0)
                {
                    return result;
                }
                
                var encoderIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedEncoderIndex");
                var encoderDisplayName = GetEncoderDisplayName(codecIndex, encoderIndex);
                var ffmpegEncoderName = _encoderMapping.GetFFmpegEncoderName(encoderDisplayName);
                
                if (string.IsNullOrEmpty(ffmpegEncoderName))
                {
                    result.AddError($"Unknown encoder: {encoderDisplayName}");
                }
                
                // 验证比特率值
                var rateModeIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedRateModeIndex");
                if (rateModeIndex >= 1 && rateModeIndex <= 3)
                {
                    var targetBitrate = context.GetData<int>("ffmpeg-video-encoder-strategy", "TargetBitrate");
                    if (targetBitrate <= 0)
                    {
                        result.AddError("Target bitrate must be greater than 0");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Validation error: {ex.Message}");
            }
            
            return result;
        }
        
        private string GetEncoderDisplayName(int codecIndex, int encoderIndex)
        {
            // 根据codecIndex和encoderIndex返回编码器显示名称
            // 这个映射应该与VideoEncoderStrategyViewModel中的逻辑一致
            return codecIndex switch
            {
                1 => encoderIndex switch // H.264
                {
                    0 => "x264",
                    1 => "NVENC H.264",
                    2 => "QSV H.264",
                    3 => "AMF H.264",
                    _ => "x264"
                },
                2 => encoderIndex switch // H.265
                {
                    0 => "x265",
                    1 => "NVENC H.265",
                    2 => "QSV H.265",
                    3 => "AMF H.265",
                    _ => "x265"
                },
                3 => "vvenc", // H.266
                4 => encoderIndex switch // AV1
                {
                    0 => "libaom-av1",
                    1 => "SVT-AV1",
                    2 => "rav1e",
                    _ => "libaom-av1"
                },
                5 => "VP9",
                6 => encoderIndex switch // ProRes
                {
                    0 => "ProRes Proxy",
                    1 => "ProRes LT",
                    2 => "ProRes Standard",
                    3 => "ProRes HQ",
                    4 => "ProRes 4444",
                    5 => "ProRes 4444 XQ",
                    _ => "ProRes Standard"
                },
                _ => "Unknown"
            };
        }
        
        private EncoderType GetEncoderType(int codecIndex, int encoderIndex)
        {
            return codecIndex switch
            {
                1 => encoderIndex switch
                {
                    0 => EncoderType.X264,
                    1 => EncoderType.NVENC,
                    2 => EncoderType.QSV,
                    _ => EncoderType.X264
                },
                2 => encoderIndex switch
                {
                    0 => EncoderType.X265,
                    1 => EncoderType.NVENC,
                    2 => EncoderType.QSV,
                    _ => EncoderType.X265
                },
                3 => EncoderType.VVC, // H.266
                4 => EncoderType.AV1,
                5 => EncoderType.VP9,
                6 => EncoderType.ProRes,
                _ => EncoderType.X264
            };
        }
        
        private bool IsX264OrX265(int codecIndex, int encoderIndex)
        {
            return (codecIndex == 1 && encoderIndex == 0) || (codecIndex == 2 && encoderIndex == 0);
        }
        
        private string GetProfileString(int codecIndex, int profileValue, int pixelFormatIndex)
        {
            if (codecIndex == 1) // H.264
            {
                // 根据像素格式自动调整profile
                var pixelFormat = GetPixelFormat(pixelFormatIndex);
                
                // 检测是否为4:4:4格式
                if (pixelFormat == "yuv444p" || pixelFormat == "yuv444p10le")
                {
                    // 4:4:4格式必须使用high444 profile
                    return profileValue switch
                    {
                        0 => "high444", // baseline → high444 (4:4:4不支持baseline)
                        1 => "high444", // main → high444 (4:4:4不支持main)
                        2 => "high444", // high → high444
                        _ => ""
                    };
                }
                // 检测是否为4:2:2格式
                else if (pixelFormat == "yuv422p" || pixelFormat == "yuv422p10le")
                {
                    // 4:2:2格式需要使用high422 profile
                    return profileValue switch
                    {
                        0 => "high422", // baseline → high422 (4:2:2不支持baseline)
                        1 => "high422", // main → high422 (4:2:2不支持main)
                        2 => "high422", // high → high422
                        _ => ""
                    };
                }
                // 4:2:0格式（yuv420p, yuv420p10le, nv12, p010le）或自动
                else
                {
                    return profileValue switch
                    {
                        0 => "baseline",
                        1 => "main",
                        2 => "high",
                        _ => ""
                    };
                }
            }
            else if (codecIndex == 2) // H.265
            {
                // 根据像素格式自动调整profile
                var pixelFormat = GetPixelFormat(pixelFormatIndex);
                
                // 检测是否为4:4:4格式
                if (pixelFormat == "yuv444p" || pixelFormat == "yuv444p10le")
                {
                    // 4:4:4格式使用main444 profile
                    return profileValue switch
                    {
                        0 => "main444-8",  // baseline → main444-8
                        1 => "main444-10", // main → main444-10
                        2 => "main444-10", // high → main444-10
                        _ => ""
                    };
                }
                // 检测是否为4:2:2格式
                else if (pixelFormat == "yuv422p" || pixelFormat == "yuv422p10le")
                {
                    // 4:2:2格式使用main422 profile
                    return profileValue switch
                    {
                        0 => "main422-10", // baseline → main422-10
                        1 => "main422-10", // main → main422-10
                        2 => "main422-10", // high → main422-10
                        _ => ""
                    };
                }
                // 4:2:0格式或自动
                else
                {
                    // 检测是否为10bit
                    bool is10bit = pixelFormat == "yuv420p10le" || pixelFormat == "p010le";
                    
                    return profileValue switch
                    {
                        0 => is10bit ? "main10" : "main",     // baseline → main/main10
                        1 => is10bit ? "main10" : "main",     // main → main/main10
                        2 => is10bit ? "main10" : "main",     // high → main/main10
                        _ => ""
                    };
                }
            }
            
            return "";
        }
        
        private string GetLevelString(int codecIndex, int levelValue)
        {
            var levels = new[] { "", "1.0", "1.1", "1.2", "1.3", "2.0", "2.1", "2.2",
                "3.0", "3.1", "3.2", "4.0", "4.1", "4.2", "5.0", "5.1", "5.2", "6.0", "6.1", "6.2" };
            
            if (levelValue >= 0 && levelValue < levels.Length)
            {
                return levels[levelValue];
            }
            return "";
        }
        
        private string GetPixelFormat(int pixelFormatIndex)
        {
            return pixelFormatIndex switch
            {
                0 => "", // 自动
                1 => "yuv420p",
                2 => "yuv420p10le",
                3 => "yuv422p",
                4 => "yuv422p10le",
                5 => "yuv444p",
                6 => "yuv444p10le",
                7 => "nv12",
                8 => "p010le",
                _ => ""
            };
        }
    }
}
