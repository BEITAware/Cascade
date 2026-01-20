using System;
using System.Collections.Generic;
using System.Linq;
using Cascade.ViewModels.Operations;
using Cascade.Services.CommandBuilders;

namespace Cascade.Services.CascadeIO
{
    /// <summary>
    /// 任务项 - 表示一个输入文件到输出文件的映射
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// 输入文件信息
        /// </summary>
        public InputFileInfo InputFile { get; set; } = new();
        
        /// <summary>
        /// 输出路径结果
        /// </summary>
        public OutputPathResult OutputPath { get; set; } = new();
        
        /// <summary>
        /// 是否有效（输入和输出都有效）
        /// </summary>
        public bool IsValid => InputFile.Exists && OutputPath.IsSuccess;
        
        /// <summary>
        /// 错误消息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// 警告消息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Cascade IO 服务 - 整合输入和输出管理
    /// 这是 Cascade IO 的主要服务接口，与具体的后端（如 FFmpeg）解耦
    /// </summary>
    public class CascadeIOService
    {
        private readonly InputFileManager _inputFileManager;
        private readonly OutputPathResolver _outputPathResolver;

        public CascadeIOService()
        {
            _inputFileManager = new InputFileManager();
            _outputPathResolver = new OutputPathResolver();
        }

        /// <summary>
        /// 为单个输入文件创建任务项
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="context">操作上下文</param>
        /// <returns>任务项</returns>
        public TaskItem CreateTask(string inputFilePath, OperationContext context)
        {
            var task = new TaskItem();

            // 获取输入文件信息
            task.InputFile = _inputFileManager.GetFileInfo(inputFilePath);

            // 验证输入文件
            var inputValidation = _inputFileManager.ValidateInputFile(inputFilePath);
            task.Errors.AddRange(inputValidation.Errors);
            task.Warnings.AddRange(inputValidation.Warnings);

            // 如果输入文件有效，解析输出路径
            if (task.InputFile.Exists)
            {
                task.OutputPath = _outputPathResolver.ResolveOutputPath(inputFilePath, context);
                
                if (!task.OutputPath.IsSuccess)
                {
                    if (!string.IsNullOrEmpty(task.OutputPath.ErrorMessage))
                    {
                        task.Errors.Add(task.OutputPath.ErrorMessage);
                    }
                }
                
                task.Warnings.AddRange(task.OutputPath.Warnings);
            }

            return task;
        }

        /// <summary>
        /// 为多个输入文件创建任务项列表
        /// </summary>
        /// <param name="inputFilePaths">输入文件路径列表</param>
        /// <param name="context">操作上下文</param>
        /// <returns>任务项列表</returns>
        public List<TaskItem> CreateTasks(IEnumerable<string> inputFilePaths, OperationContext context)
        {
            return inputFilePaths.Select(path => CreateTask(path, context)).ToList();
        }

        /// <summary>
        /// 验证 Cascade IO 配置
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();

            // 验证输出配置
            var outputValidation = _outputPathResolver.ValidateConfiguration(context);
            result.Merge(outputValidation);

            return result;
        }

        /// <summary>
        /// 获取输入文件管理器
        /// </summary>
        public InputFileManager InputFileManager => _inputFileManager;

        /// <summary>
        /// 获取输出路径解析器
        /// </summary>
        public OutputPathResolver OutputPathResolver => _outputPathResolver;

        /// <summary>
        /// 从 MediaViewModel 获取选中的文件路径
        /// </summary>
        /// <param name="mediaViewModel">媒体视图模型</param>
        /// <returns>选中的文件路径列表</returns>
        public static List<string> GetSelectedFilePaths(object? mediaViewModel)
        {
            var paths = new List<string>();

            if (mediaViewModel == null)
                return paths;

            try
            {
                // 使用反射获取 SelectedMediaItems
                var type = mediaViewModel.GetType();
                var selectedItemsProperty = type.GetProperty("SelectedMediaItems");
                
                if (selectedItemsProperty != null)
                {
                    var selectedItems = selectedItemsProperty.GetValue(mediaViewModel);
                    
                    if (selectedItems is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            var itemType = item.GetType();
                            var filePathProperty = itemType.GetProperty("FilePath");
                            
                            if (filePathProperty != null)
                            {
                                var filePath = filePathProperty.GetValue(item) as string;
                                if (!string.IsNullOrEmpty(filePath))
                                {
                                    paths.Add(filePath);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting selected file paths: {ex.Message}");
            }

            return paths;
        }

        /// <summary>
        /// 确保输出目录存在
        /// </summary>
        /// <param name="outputDirectory">输出目录路径</param>
        /// <returns>是否成功创建或已存在</returns>
        public bool EnsureOutputDirectoryExists(string outputDirectory)
        {
            try
            {
                if (!System.IO.Directory.Exists(outputDirectory))
                {
                    System.IO.Directory.CreateDirectory(outputDirectory);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating output directory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量确保输出目录存在
        /// </summary>
        /// <param name="tasks">任务项列表</param>
        /// <returns>所有目录是否都成功创建或已存在</returns>
        public bool EnsureOutputDirectoriesExist(IEnumerable<TaskItem> tasks)
        {
            var directories = tasks
                .Where(t => t.IsValid)
                .Select(t => t.OutputPath.Directory)
                .Distinct()
                .ToList();

            return directories.All(EnsureOutputDirectoryExists);
        }
    }
}
