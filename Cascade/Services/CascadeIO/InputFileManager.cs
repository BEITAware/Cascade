using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cascade.ViewModels.Operations;
using Cascade.Services.CommandBuilders;

namespace Cascade.Services.CascadeIO
{
    /// <summary>
    /// 输入文件信息
    /// </summary>
    public class InputFileInfo
    {
        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件名（含扩展名）
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件目录
        /// </summary>
        public string Directory { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// 是否存在
        /// </summary>
        public bool Exists { get; set; }
        
        /// <summary>
        /// 是否为视频文件
        /// </summary>
        public bool IsVideoFile { get; set; }
    }

    /// <summary>
    /// 输入文件管理器 - 负责管理和验证输入文件
    /// 与具体的后端（如 FFmpeg）解耦
    /// </summary>
    public class InputFileManager
    {
        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v",
            ".mpg", ".mpeg", ".3gp", ".3g2", ".mts", ".m2ts", ".ts", ".vob",
            ".ogv", ".f4v", ".rm", ".rmvb", ".asf", ".divx"
        };

        /// <summary>
        /// 获取输入文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>输入文件信息</returns>
        public InputFileInfo GetFileInfo(string filePath)
        {
            var info = new InputFileInfo
            {
                FullPath = filePath,
                Exists = File.Exists(filePath)
            };

            if (info.Exists)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    info.FileName = fileInfo.Name;
                    info.Directory = fileInfo.DirectoryName ?? string.Empty;
                    info.Extension = fileInfo.Extension;
                    info.FileSize = fileInfo.Length;
                    info.IsVideoFile = IsVideoFile(fileInfo.Extension);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting file info: {ex.Message}");
                }
            }
            else
            {
                info.FileName = Path.GetFileName(filePath);
                info.Directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                info.Extension = Path.GetExtension(filePath);
                info.IsVideoFile = IsVideoFile(info.Extension);
            }

            return info;
        }

        /// <summary>
        /// 批量获取输入文件信息
        /// </summary>
        /// <param name="filePaths">文件路径列表</param>
        /// <returns>输入文件信息列表</returns>
        public List<InputFileInfo> GetFileInfos(IEnumerable<string> filePaths)
        {
            return filePaths.Select(GetFileInfo).ToList();
        }

        /// <summary>
        /// 验证输入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateInputFile(string filePath)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.AddError("Input file path is empty.");
                return result;
            }

            var fileInfo = GetFileInfo(filePath);

            if (!fileInfo.Exists)
            {
                result.AddError($"Input file does not exist: {filePath}");
                return result;
            }

            if (!fileInfo.IsVideoFile)
            {
                result.AddWarning($"File may not be a video file: {filePath}");
            }

            if (fileInfo.FileSize == 0)
            {
                result.AddWarning($"Input file is empty: {filePath}");
            }

            return result;
        }

        /// <summary>
        /// 批量验证输入文件
        /// </summary>
        /// <param name="filePaths">文件路径列表</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateInputFiles(IEnumerable<string> filePaths)
        {
            var result = new ValidationResult();

            var filePathList = filePaths.ToList();
            
            if (!filePathList.Any())
            {
                result.AddError("No input files specified.");
                return result;
            }

            foreach (var filePath in filePathList)
            {
                var fileResult = ValidateInputFile(filePath);
                result.Merge(fileResult);
            }

            return result;
        }

        /// <summary>
        /// 判断是否为视频文件
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <returns>是否为视频文件</returns>
        public bool IsVideoFile(string extension)
        {
            return VideoExtensions.Contains(extension);
        }

        /// <summary>
        /// 获取支持的视频扩展名列表
        /// </summary>
        /// <returns>视频扩展名列表</returns>
        public IReadOnlySet<string> GetSupportedVideoExtensions()
        {
            return VideoExtensions;
        }

        /// <summary>
        /// 过滤出视频文件
        /// </summary>
        /// <param name="filePaths">文件路径列表</param>
        /// <returns>视频文件路径列表</returns>
        public List<string> FilterVideoFiles(IEnumerable<string> filePaths)
        {
            return filePaths
                .Where(path => IsVideoFile(Path.GetExtension(path)))
                .ToList();
        }
    }
}
