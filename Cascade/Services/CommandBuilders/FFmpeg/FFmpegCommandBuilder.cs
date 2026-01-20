using System;
using System.Collections.Generic;
using System.Linq;
using Cascade.Services.CommandBuilders.FFmpeg.Providers;
using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders.FFmpeg
{
    /// <summary>
    /// FFmpeg命令构建器
    /// </summary>
    public class FFmpegCommandBuilder : CommandBuilderBase
    {
        public override BackendType SupportedBackend => BackendType.FFmpeg;
        public override string DisplayName => GetLocalizedString("Backend_FFmpeg");
        
        public FFmpegCommandBuilder()
        {
        }
        
        /// <summary>
        /// 注册命令片段提供者
        /// </summary>
        /// <param name="provider">片段提供者</param>
        public void RegisterSegmentProvider(ICommandSegmentProvider provider)
        {
            FFmpegProviderRegistry.Instance.Register(provider);
        }
        
        /// <summary>
        /// 注销命令片段提供者
        /// </summary>
        /// <param name="provider">片段提供者</param>
        /// <returns>是否成功注销</returns>
        public bool UnregisterSegmentProvider(ICommandSegmentProvider provider)
        {
            return FFmpegProviderRegistry.Instance.Unregister(provider);
        }
        
        protected override void RegisterParameterMappers()
        {
            // 这里将在后续任务中实现具体的参数映射器
            // ParameterMappers["ffmpeg-video-encoder-strategy"] = new VideoEncoderParameterMapper();
            // ParameterMappers["ffmpeg-size-layout"] = new SizeLayoutParameterMapper();
            // ParameterMappers["ffmpeg-muxing-delivery"] = new MuxingDeliveryParameterMapper();
        }
        
        public override CommandResult GenerateCommand(OperationContext context)
        {
            Log("Starting command generation...");

            // 检查输出路径
            var outputPath = context.GetData<string>("cascade-io-naming-output", "outputPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                Log("Output path is missing or empty.");
                var result = new ValidationResult();
                result.AddError("Output path is not set.");
                return new CommandResult
                {
                    Backend = SupportedBackend,
                    Content = string.Empty,
                    ValidationResult = result,
                    PreviewText = string.Empty
                };
            }

            var segments = new List<CommandSegment>();
            var validationResult = new ValidationResult();
            
            try
            {
                // 获取并记录提供者数量
                var allProviders = FFmpegProviderRegistry.Instance.GetAllProviders();
                var totalProviders = allProviders.Values.Sum(p => p.Count);
                Log($"Found {totalProviders} providers.");

                // 按类型和优先级收集所有片段
                foreach (var segmentType in Enum.GetValues<CommandSegmentType>().OrderBy(t => (int)t))
                {
                    var providers = FFmpegProviderRegistry.Instance.GetProviders(segmentType);
                    
                    // 冲突处理：对于某些类型，只使用优先级最高的有效提供者
                    bool isSingleProviderType = segmentType == CommandSegmentType.VideoCodec ||
                                                segmentType == CommandSegmentType.AudioCodec;
                    
                    foreach (var provider in providers)
                    {
                        Log($"Processing provider: {provider.GetType().Name}");
                        try
                        {
                            var segment = provider.GenerateSegment(context);
                            if (segment.IsValid && !string.IsNullOrEmpty(segment.Parameters))
                            {
                                segments.Add(segment);
                                if (isSingleProviderType) break; // 找到最高优先级的有效片段，停止处理该类型
                            }
                            else if (!segment.IsValid)
                            {
                                validationResult.AddError($"Segment {segmentType}: {segment.ValidationMessage}");
                            }
                            
                            // 合并验证结果
                            var segmentValidation = provider.ValidateConfiguration(context);
                            validationResult.Merge(segmentValidation);
                        }
                        catch (Exception ex)
                        {
                            Log($"Error generating {segmentType} segment: {ex.Message}");
                            validationResult.AddError($"Error generating {segmentType} segment: {ex.Message}");
                        }
                    }
                }
                
                // 组装命令
                var commandLine = "ffmpeg";
                if (segments.Any())
                {
                    commandLine += " " + string.Join(" ", segments.Select(s => s.Parameters));
                }
                
                Log("Command generation completed successfully.");

                return new CommandResult
                {
                    Backend = SupportedBackend,
                    Content = commandLine,
                    ValidationResult = validationResult,
                    PreviewText = string.Empty // Preview will be generated separately when needed
                };
            }
            catch (Exception ex)
            {
                Log($"Command generation failed: {ex.Message}");
                validationResult.AddError($"Exception: {ex.Message}");
                return new CommandResult
                {
                    Backend = SupportedBackend,
                    Content = string.Empty,
                    ValidationResult = validationResult,
                    PreviewText = string.Empty
                };
            }
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[FFmpegCommandBuilder] {message}");
        }
        
        public override ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();
            
            // 验证所有注册的提供者
            var allProviders = FFmpegProviderRegistry.Instance.GetAllProviders();
            foreach (var providerGroup in allProviders.Values)
            {
                foreach (var provider in providerGroup)
                {
                    try
                    {
                        var providerResult = provider.ValidateConfiguration(context);
                        result.Merge(providerResult);
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Validation error in {provider.GetType().Name}: {ex.Message}");
                    }
                }
            }
            
            return result;
        }
        
        public override string GetPreview(OperationContext context)
        {
            try
            {
                var segments = new List<CommandSegment>();
                var warnings = new List<string>();
                
                // 按类型和优先级收集所有片段（与GenerateCommand相同的逻辑，但不调用GenerateCommand）
                foreach (var segmentType in Enum.GetValues<CommandSegmentType>().OrderBy(t => (int)t))
                {
                    var providers = FFmpegProviderRegistry.Instance.GetProviders(segmentType);
                    
                    // 冲突处理：对于某些类型，只使用优先级最高的有效提供者
                    bool isSingleProviderType = segmentType == CommandSegmentType.VideoCodec ||
                                                segmentType == CommandSegmentType.AudioCodec;

                    foreach (var provider in providers)
                    {
                        try
                        {
                            var segment = provider.GenerateSegment(context);
                            if (segment.IsValid && !string.IsNullOrEmpty(segment.Parameters))
                            {
                                segments.Add(segment);
                                if (isSingleProviderType) break; // 找到最高优先级的有效片段，停止处理该类型
                            }
                            
                            // 收集验证警告
                            var segmentValidation = provider.ValidateConfiguration(context);
                            warnings.AddRange(segmentValidation.Warnings);
                        }
                        catch (Exception ex)
                        {
                            warnings.Add($"Error generating {segmentType} segment: {ex.Message}");
                        }
                    }
                }
                
                // 组装命令
                var commandLine = "ffmpeg";
                if (segments.Any())
                {
                    commandLine += " " + string.Join(" ", segments.Select(s => s.Parameters));
                }
                
                // 添加换行符以提高可读性
                var formattedCommand = commandLine
                    .Replace(" -", "\n  -")
                    .Replace("ffmpeg\n  ", "ffmpeg ");
                
                // 如果有验证警告，添加到预览中
                if (warnings.Any())
                {
                    formattedCommand += "\n\n# Warnings:\n";
                    foreach (var warning in warnings)
                    {
                        formattedCommand += $"# - {warning}\n";
                    }
                }
                
                return formattedCommand;
            }
            catch (Exception ex)
            {
                return $"# Error generating preview: {ex.Message}";
            }
        }
        
        public override IReadOnlyList<string> GetSupportedPages()
        {
            return new[]
            {
                "ffmpeg-video-encoder-strategy",
                "ffmpeg-size-layout", 
                "ffmpeg-muxing-delivery",
                "cascade-io-naming-output"
            };
        }
    }
}