using System;
using System.IO;
using Cascade.ViewModels.Operations;
using Cascade.Services.CascadeIO;

namespace Cascade.Services.CommandBuilders.FFmpeg.Providers
{
    /// <summary>
    /// 输出文件命令片段提供者
    /// 使用 CascadeIOService 来解析输出路径，与 Cascade IO 解耦
    /// </summary>
    public class OutputFileSegmentProvider : ICommandSegmentProvider
    {
        private readonly CascadeIOService _ioService;

        public CommandSegmentType SegmentType => CommandSegmentType.OutputFile;
        public int Priority => 0;

        public OutputFileSegmentProvider()
        {
            _ioService = new CascadeIOService();
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
                // 首先验证 Cascade IO 配置
                var ioValidation = _ioService.ValidateConfiguration(context);
                if (!ioValidation.IsValid)
                {
                    segment.IsValid = false;
                    segment.ValidationMessage = string.Join("; ", ioValidation.Errors);
                    return segment;
                }

                // 获取输入文件路径（从 context 中获取，由调用方设置）
                var inputFilePath = context.GetData<string>("cascade-io-input", "currentInputFile", string.Empty);
                
                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    // 如果没有设置输入文件，回退到旧的行为（直接使用 outputPath 和 outputFileName）
                    var outputPath = context.GetData<string>("cascade-io-naming-output", "outputPath", string.Empty);
                    var outputFileName = context.GetData<string>("cascade-io-naming-output", "outputFileName", string.Empty);

                    if (string.IsNullOrWhiteSpace(outputPath))
                    {
                        segment.IsValid = false;
                        segment.ValidationMessage = "Output path is not set.";
                        return segment;
                    }

                    if (string.IsNullOrWhiteSpace(outputFileName))
                    {
                        segment.IsValid = false;
                        segment.ValidationMessage = "Output file name is not set.";
                        return segment;
                    }

                    var fullPath = Path.Combine(outputPath, outputFileName);
                    segment.Parameters = $"\"{fullPath}\"";
                    segment.IsValid = true;
                }
                else
                {
                    // 使用 CascadeIOService 解析输出路径
                    var outputResult = _ioService.OutputPathResolver.ResolveOutputPath(inputFilePath, context);
                    
                    if (!outputResult.IsSuccess)
                    {
                        segment.IsValid = false;
                        segment.ValidationMessage = outputResult.ErrorMessage ?? "Failed to resolve output path.";
                        return segment;
                    }

                    // 处理路径中的空格，添加引号
                    segment.Parameters = $"\"{outputResult.FullPath}\"";
                    segment.IsValid = true;
                }
            }
            catch (Exception ex)
            {
                segment.IsValid = false;
                segment.ValidationMessage = $"Error generating output file segment: {ex.Message}";
            }

            return segment;
        }

        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();

            try
            {
                // 使用 CascadeIOService 验证配置
                var ioValidation = _ioService.ValidateConfiguration(context);
                result.Merge(ioValidation);

                // 额外验证：确保至少有输出路径或输入文件
                var inputFilePath = context.GetData<string>("cascade-io-input", "currentInputFile", string.Empty);
                var outputPath = context.GetData<string>("cascade-io-naming-output", "outputPath", string.Empty);
                var outputFileName = context.GetData<string>("cascade-io-naming-output", "outputFileName", string.Empty);

                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    // 如果没有输入文件，必须有输出路径和文件名
                    if (string.IsNullOrWhiteSpace(outputPath))
                    {
                        result.AddError("Output path is required.");
                    }

                    if (string.IsNullOrWhiteSpace(outputFileName))
                    {
                        result.AddError("Output file name is required.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Validation error in OutputFileSegmentProvider: {ex.Message}");
            }

            return result;
        }
    }
}
