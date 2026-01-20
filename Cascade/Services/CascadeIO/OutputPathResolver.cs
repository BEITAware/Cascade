using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cascade.ViewModels.Operations;
using Cascade.Services.CommandBuilders;

namespace Cascade.Services.CascadeIO
{
    /// <summary>
    /// 输出模式枚举
    /// </summary>
    public enum OutputMode
    {
        /// <summary>
        /// 输出到指定目录
        /// </summary>
        SpecifiedDirectory = 0,
        
        /// <summary>
        /// 输出到源文件夹
        /// </summary>
        SourceFolder = 1,
        
        /// <summary>
        /// 输出到源文件夹子目录
        /// </summary>
        SourceSubdirectory = 2,
        
        /// <summary>
        /// 克隆源文件夹结构
        /// </summary>
        CloneStructure = 3
    }

    /// <summary>
    /// 文件名冲突处理策略枚举
    /// </summary>
    public enum ConflictResolution
    {
        /// <summary>
        /// 重命名（询问用户）
        /// </summary>
        Rename = 0,
        
        /// <summary>
        /// 跳过
        /// </summary>
        Skip = 1,
        
        /// <summary>
        /// 添加数字后缀
        /// </summary>
        AddNumericSuffix = 2,
        
        /// <summary>
        /// 添加时间戳后缀
        /// </summary>
        AddTimestampSuffix = 3,
        
        /// <summary>
        /// 添加自定义后缀
        /// </summary>
        AddCustomSuffix = 4,
        
        /// <summary>
        /// 覆盖
        /// </summary>
        Overwrite = 5
    }

    /// <summary>
    /// 输出路径解析结果
    /// </summary>
    public class OutputPathResult
    {
        /// <summary>
        /// 完整的输出文件路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 输出目录
        /// </summary>
        public string Directory { get; set; } = string.Empty;
        
        /// <summary>
        /// 输出文件名（含扩展名）
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 警告消息
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// 输出路径解析器 - 负责根据输入文件和配置生成输出路径
    /// 这是 Cascade IO 的核心服务，与具体的后端（如 FFmpeg）解耦
    /// </summary>
    public class OutputPathResolver
    {
        /// <summary>
        /// 解析单个输入文件的输出路径
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="context">操作上下文</param>
        /// <returns>输出路径解析结果</returns>
        public OutputPathResult ResolveOutputPath(string inputFilePath, OperationContext context)
        {
            var result = new OutputPathResult();

            try
            {
                // 验证输入文件
                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    result.ErrorMessage = "Input file path is empty.";
                    return result;
                }

                if (!File.Exists(inputFilePath))
                {
                    result.ErrorMessage = $"Input file does not exist: {inputFilePath}";
                    return result;
                }

                // 获取配置
                var outputMode = (OutputMode)context.GetData<int>("cascade-io-naming-output", "SelectedOutputModeIndex", 0);
                var outputPath = context.GetData<string>("cascade-io-naming-output", "outputPath", string.Empty);
                var subdirectoryName = context.GetData<string>("cascade-io-naming-output", "subdirectoryName", "output");
                var outputFileName = context.GetData<string>("cascade-io-naming-output", "outputFileName", string.Empty);
                var conflictResolution = (ConflictResolution)context.GetData<int>("cascade-io-naming-output", "SelectedConflictResolutionIndex", 0);
                var customSuffix = context.GetData<string>("cascade-io-naming-output", "customSuffix", "_copy");

                // 确定输出目录
                string outputDirectory = DetermineOutputDirectory(inputFilePath, outputMode, outputPath, subdirectoryName);
                
                if (string.IsNullOrWhiteSpace(outputDirectory))
                {
                    result.ErrorMessage = "Output directory could not be determined.";
                    return result;
                }

                // 确定输出文件名
                string finalFileName = DetermineOutputFileName(inputFilePath, outputFileName);
                
                if (string.IsNullOrWhiteSpace(finalFileName))
                {
                    result.ErrorMessage = "Output file name could not be determined.";
                    return result;
                }

                // 组合完整路径
                string fullPath = Path.Combine(outputDirectory, finalFileName);

                // 处理文件名冲突
                fullPath = ResolveConflict(fullPath, conflictResolution, customSuffix, result);

                // 设置结果
                result.FullPath = fullPath;
                result.Directory = outputDirectory;
                result.FileName = Path.GetFileName(fullPath);
                result.IsSuccess = true;

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error resolving output path: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 批量解析多个输入文件的输出路径
        /// </summary>
        /// <param name="inputFilePaths">输入文件路径列表</param>
        /// <param name="context">操作上下文</param>
        /// <returns>输出路径解析结果字典（输入路径 -> 输出路径结果）</returns>
        public Dictionary<string, OutputPathResult> ResolveOutputPaths(IEnumerable<string> inputFilePaths, OperationContext context)
        {
            var results = new Dictionary<string, OutputPathResult>();

            foreach (var inputPath in inputFilePaths)
            {
                results[inputPath] = ResolveOutputPath(inputPath, context);
            }

            return results;
        }

        /// <summary>
        /// 确定输出目录
        /// </summary>
        private string DetermineOutputDirectory(string inputFilePath, OutputMode mode, string specifiedPath, string subdirectoryName)
        {
            var inputDirectory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;

            return mode switch
            {
                OutputMode.SpecifiedDirectory => specifiedPath,
                OutputMode.SourceFolder => inputDirectory,
                OutputMode.SourceSubdirectory => Path.Combine(inputDirectory, subdirectoryName),
                OutputMode.CloneStructure => specifiedPath, // 克隆结构时使用指定路径作为根目录
                _ => string.Empty
            };
        }

        /// <summary>
        /// 确定输出文件名
        /// </summary>
        private string DetermineOutputFileName(string inputFilePath, string customFileName)
        {
            // 如果用户指定了输出文件名，使用它
            if (!string.IsNullOrWhiteSpace(customFileName))
            {
                return customFileName;
            }

            // 否则使用输入文件名
            return Path.GetFileName(inputFilePath);
        }

        /// <summary>
        /// 解决文件名冲突
        /// </summary>
        private string ResolveConflict(string fullPath, ConflictResolution resolution, string customSuffix, OutputPathResult result)
        {
            // 如果文件不存在，直接返回
            if (!File.Exists(fullPath))
            {
                return fullPath;
            }

            switch (resolution)
            {
                case ConflictResolution.Skip:
                    result.Warnings.Add($"File already exists and will be skipped: {fullPath}");
                    return fullPath;

                case ConflictResolution.Overwrite:
                    result.Warnings.Add($"File already exists and will be overwritten: {fullPath}");
                    return fullPath;

                case ConflictResolution.AddNumericSuffix:
                    return AddNumericSuffix(fullPath);

                case ConflictResolution.AddTimestampSuffix:
                    return AddTimestampSuffix(fullPath);

                case ConflictResolution.AddCustomSuffix:
                    return AddCustomSuffix(fullPath, customSuffix);

                case ConflictResolution.Rename:
                default:
                    result.Warnings.Add($"File already exists: {fullPath}. Manual rename required.");
                    return fullPath;
            }
        }

        /// <summary>
        /// 添加数字后缀
        /// </summary>
        private string AddNumericSuffix(string fullPath)
        {
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);

            int counter = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        /// <summary>
        /// 添加时间戳后缀
        /// </summary>
        private string AddTimestampSuffix(string fullPath)
        {
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
        }

        /// <summary>
        /// 添加自定义后缀
        /// </summary>
        private string AddCustomSuffix(string fullPath, string suffix)
        {
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);

            var newPath = Path.Combine(directory, $"{fileNameWithoutExt}{suffix}{extension}");
            
            // 如果添加后缀后仍然冲突，添加数字后缀
            if (File.Exists(newPath))
            {
                return AddNumericSuffix(newPath);
            }

            return newPath;
        }

        /// <summary>
        /// 验证输出配置
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();

            try
            {
                var outputMode = (OutputMode)context.GetData<int>("cascade-io-naming-output", "SelectedOutputModeIndex", 0);
                var outputPath = context.GetData<string>("cascade-io-naming-output", "outputPath", string.Empty);
                var subdirectoryName = context.GetData<string>("cascade-io-naming-output", "subdirectoryName", "output");

                // 验证输出模式
                if (outputMode == OutputMode.SpecifiedDirectory || outputMode == OutputMode.CloneStructure)
                {
                    if (string.IsNullOrWhiteSpace(outputPath))
                    {
                        result.AddError("Output path is required for the selected output mode.");
                    }
                    else if (!Directory.Exists(outputPath))
                    {
                        result.AddWarning($"Output directory does not exist and will be created: {outputPath}");
                    }
                }

                if (outputMode == OutputMode.SourceSubdirectory)
                {
                    if (string.IsNullOrWhiteSpace(subdirectoryName))
                    {
                        result.AddError("Subdirectory name is required for the selected output mode.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Validation error: {ex.Message}");
            }

            return result;
        }
    }
}
