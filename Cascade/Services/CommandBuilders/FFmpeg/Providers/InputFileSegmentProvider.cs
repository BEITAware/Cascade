using System;
using System.IO;
using Cascade.ViewModels.Operations;
using Cascade.Services.CascadeIO;

namespace Cascade.Services.CommandBuilders.FFmpeg.Providers
{
    /// <summary>
    /// 输入文件命令片段提供者
    /// 使用 CascadeIOService 来验证输入文件
    /// </summary>
    public class InputFileSegmentProvider : ICommandSegmentProvider
    {
        private readonly CascadeIOService _ioService;

        public CommandSegmentType SegmentType => CommandSegmentType.Input;
        public int Priority => 0;

        public InputFileSegmentProvider()
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
                // 获取输入文件路径
                var inputFilePath = context.GetData<string>("cascade-io-input", "currentInputFile", string.Empty);
                
                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    segment.IsValid = false;
                    segment.ValidationMessage = "Input file is not set.";
                    return segment;
                }

                // 验证输入文件
                var fileInfo = _ioService.InputFileManager.GetFileInfo(inputFilePath);
                if (!fileInfo.Exists)
                {
                    segment.IsValid = false;
                    segment.ValidationMessage = $"Input file does not exist: {inputFilePath}";
                    return segment;
                }

                // 处理路径中的空格，添加引号
                segment.Parameters = $"-i \"{inputFilePath}\"";
                segment.IsValid = true;
            }
            catch (Exception ex)
            {
                segment.IsValid = false;
                segment.ValidationMessage = $"Error generating input file segment: {ex.Message}";
            }

            return segment;
        }

        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();

            try
            {
                var inputFilePath = context.GetData<string>("cascade-io-input", "currentInputFile", string.Empty);
                
                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    result.AddError("Input file is required.");
                    return result;
                }

                // 使用 InputFileManager 验证输入文件
                var validation = _ioService.InputFileManager.ValidateInputFile(inputFilePath);
                result.Merge(validation);
            }
            catch (Exception ex)
            {
                result.AddError($"Validation error in InputFileSegmentProvider: {ex.Message}");
            }

            return result;
        }
    }
}
