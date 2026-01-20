using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Cascade.Models;
using Cascade.Services.CommandBuilders;
using Cascade.ViewModels.Operations;

namespace Cascade.ViewModels
{
    /// <summary>
    /// 任务页面ViewModel - 显示所有媒体并生成命令
    /// </summary>
    public class TasksViewModel : ViewModelBase
    {
        private string _commandPreview = string.Empty;
        private string _selectedBackend = string.Empty;
        private string _outputPath = string.Empty;
        private string _videoEncoder = string.Empty;
        private string _videoMode = string.Empty;
        private string _outputSize = string.Empty;
        private string _outputFormat = string.Empty;
        private bool _hasMedia = false;
        private MediaItem? _selectedMediaItem;

        public TasksViewModel()
        {
            CopyCommandCommand = new RelayCommand(ExecuteCopyCommand, CanExecuteCopyCommand);
            RefreshPreviewCommand = new RelayCommand(ExecuteRefreshPreview);
            AddToQueueCommand = new RelayCommand(ExecuteAddToQueue, CanExecuteAddToQueue);

            // 初始化时刷新预览
            RefreshPreview();
        }

        /// <summary>
        /// 所有媒体项集合（从MediaViewModel同步）
        /// </summary>
        public ObservableCollection<MediaItem> MediaItems { get; } = new ObservableCollection<MediaItem>();

        /// <summary>
        /// 当前选中的媒体项（单选）
        /// </summary>
        public MediaItem? SelectedMediaItem
        {
            get => _selectedMediaItem;
            set
            {
                if (SetProperty(ref _selectedMediaItem, value))
                {
                    // 选择变化时自动刷新预览
                    RefreshPreview();
                }
            }
        }

        /// <summary>
        /// 是否有媒体
        /// </summary>
        public bool HasMedia
        {
            get => _hasMedia;
            set => SetProperty(ref _hasMedia, value);
        }

        /// <summary>
        /// 命令预览文本
        /// </summary>
        public string CommandPreview
        {
            get => _commandPreview;
            set => SetProperty(ref _commandPreview, value);
        }

        /// <summary>
        /// 选中的后端
        /// </summary>
        public string SelectedBackend
        {
            get => _selectedBackend;
            set => SetProperty(ref _selectedBackend, value);
        }

        /// <summary>
        /// 输出路径
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        /// <summary>
        /// 视频编码器
        /// </summary>
        public string VideoEncoder
        {
            get => _videoEncoder;
            set => SetProperty(ref _videoEncoder, value);
        }

        /// <summary>
        /// 视频编码模式
        /// </summary>
        public string VideoMode
        {
            get => _videoMode;
            set => SetProperty(ref _videoMode, value);
        }

        /// <summary>
        /// 输出尺寸
        /// </summary>
        public string OutputSize
        {
            get => _outputSize;
            set => SetProperty(ref _outputSize, value);
        }

        /// <summary>
        /// 输出格式
        /// </summary>
        public string OutputFormat
        {
            get => _outputFormat;
            set => SetProperty(ref _outputFormat, value);
        }

        /// <summary>
        /// 额外的设定项
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> AdditionalSettings { get; } = new ObservableCollection<KeyValuePair<string, string>>();

        // 命令
        public ICommand CopyCommandCommand { get; }
        public ICommand RefreshPreviewCommand { get; }
        public ICommand AddToQueueCommand { get; }

        /// <summary>
        /// 刷新预览
        /// </summary>
        public void RefreshPreview()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("=== TasksViewModel.RefreshPreview() START ===");
#endif
            try
            {
                var context = OperationContext.Instance;
                
                // 设置当前输入文件（如果有选中的媒体）
                if (SelectedMediaItem != null)
                {
                    context.PublishData("cascade-io-input", "currentInputFile", SelectedMediaItem.FilePath);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Set current input file: {SelectedMediaItem.FilePath}");
#endif
                }
                else
                {
                    // 清除输入文件
                    context.PublishData("cascade-io-input", "currentInputFile", string.Empty);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("No media selected, cleared input file");
#endif
                }
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"OperationContext.SelectedBackend: {context.SelectedBackend}");
#endif
                
                // 更新后端信息
                var backendType = context.SelectedBackend;
                SelectedBackend = GetLocalizedBackendName(backendType);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"SelectedBackend (localized): {SelectedBackend}");
#endif

                // 获取输出路径
                var outputPath = context.GetData<string>("cascade-io-naming-output", "outputPath");
                var outputFileName = context.GetData<string>("cascade-io-naming-output", "outputFileName");
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Output Path: {outputPath}");
                System.Diagnostics.Debug.WriteLine($"Output FileName: {outputFileName}");
#endif
                
                if (!string.IsNullOrEmpty(outputPath) && !string.IsNullOrEmpty(outputFileName))
                {
                    OutputPath = System.IO.Path.Combine(outputPath, outputFileName);
                }
                else if (!string.IsNullOrEmpty(outputPath))
                {
                    OutputPath = outputPath;
                }
                else
                {
                    OutputPath = Services.LocalizationService.GetString("Tasks_NotSet");
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"OutputPath (final): {OutputPath}");
#endif

                // 获取视频编码器信息
                var codecIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedCodecIndex");
                var encoderIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedEncoderIndex");
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"CodecIndex: {codecIndex}, EncoderIndex: {encoderIndex}");
#endif
                
                if (codecIndex == 0) // 复制流
                {
                    VideoMode = Services.LocalizationService.GetString("Tasks_StreamCopy");
                    VideoEncoder = Services.LocalizationService.GetString("Tasks_StreamCopy");
                }
                else
                {
                    VideoMode = Services.LocalizationService.GetString("Tasks_Encode");
                    VideoEncoder = GetEncoderDisplayName(codecIndex, encoderIndex);
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"VideoMode: {VideoMode}, VideoEncoder: {VideoEncoder}");
#endif

                // 获取输出尺寸
                var outputSizeMode = context.GetData<int>("ffmpeg-size-layout", "OutputSizeMode");
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"OutputSizeMode: {outputSizeMode}");
#endif
                
                if (outputSizeMode == 0) // 自动
                {
                    OutputSize = Services.LocalizationService.GetString("Tasks_SameAsSource");
                }
                else if (outputSizeMode == 1) // 自定义
                {
                    var width = context.GetData<string>("ffmpeg-size-layout", "CustomWidth");
                    var height = context.GetData<string>("ffmpeg-size-layout", "CustomHeight");
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Custom Size - Width: {width}, Height: {height}");
#endif
                    
                    if (!string.IsNullOrEmpty(width) && !string.IsNullOrEmpty(height))
                    {
                        OutputSize = $"{width} × {height}";
                    }
                    else
                    {
                        OutputSize = Services.LocalizationService.GetString("Tasks_NotSet");
                    }
                }
                else
                {
                    OutputSize = Services.LocalizationService.GetString("Tasks_NotSet");
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"OutputSize (final): {OutputSize}");
#endif

                // 获取输出格式
                var containerIndex = context.GetData<int>("ffmpeg-muxing-delivery", "SelectedContainerIndex");
                OutputFormat = GetContainerDisplayName(containerIndex);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"ContainerIndex: {containerIndex}, OutputFormat: {OutputFormat}");
#endif

                // 清空并重新填充额外设定
                AdditionalSettings.Clear();

                // 添加预设信息
                if (codecIndex != 0 && codecIndex != 7) // 非复制流且非无压缩
                {
                    var presetValue = context.GetData<int>("ffmpeg-video-encoder-strategy", "PresetValue");
                    var presetLabel = GetPresetLabel(codecIndex, encoderIndex, presetValue);
                    if (!string.IsNullOrEmpty(presetLabel))
                    {
                        AdditionalSettings.Add(new KeyValuePair<string, string>(
                            Services.LocalizationService.GetString("Tasks_Preset"), 
                            presetLabel));
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Added Preset: {presetLabel}");
#endif
                    }
                }

                // 添加码率模式信息
                if (codecIndex != 0) // 非复制流
                {
                    var rateModeIndex = context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedRateModeIndex");
                    var rateModeLabel = GetRateModeLabel(rateModeIndex);
                    if (!string.IsNullOrEmpty(rateModeLabel))
                    {
                        AdditionalSettings.Add(new KeyValuePair<string, string>(
                            Services.LocalizationService.GetString("Tasks_RateMode"), 
                            rateModeLabel));
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Added RateMode: {rateModeLabel}");
#endif
                    }

                    // 添加具体的码率值
                    if (rateModeIndex == 0) // CRF
                    {
                        var crfValue = context.GetData<int>("ffmpeg-video-encoder-strategy", "CrfValue");
                        AdditionalSettings.Add(new KeyValuePair<string, string>("CRF", crfValue.ToString()));
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Added CRF: {crfValue}");
#endif
                    }
                    else if (rateModeIndex >= 1 && rateModeIndex <= 3) // 比特率模式
                    {
                        var targetBitrate = context.GetData<int>("ffmpeg-video-encoder-strategy", "TargetBitrate");
                        AdditionalSettings.Add(new KeyValuePair<string, string>(
                            Services.LocalizationService.GetString("Tasks_Bitrate"), 
                            $"{targetBitrate} kbps"));
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Added Bitrate: {targetBitrate} kbps");
#endif
                    }
                }

                // 添加滤镜信息
                var hasFilters = false;
                var filterList = new List<string>();

                // 检查缩放
                if (outputSizeMode == 1)
                {
                    filterList.Add(Services.LocalizationService.GetString("Tasks_Filter_Scale"));
                    hasFilters = true;
                }

                // 检查裁切
                var cropMode = context.GetData<int>("ffmpeg-size-layout", "CropMode");
                if (cropMode == 2)
                {
                    var cropTop = context.GetData<int>("ffmpeg-size-layout", "CropTop");
                    var cropBottom = context.GetData<int>("ffmpeg-size-layout", "CropBottom");
                    var cropLeft = context.GetData<int>("ffmpeg-size-layout", "CropLeft");
                    var cropRight = context.GetData<int>("ffmpeg-size-layout", "CropRight");
                    
                    if (cropTop > 0 || cropBottom > 0 || cropLeft > 0 || cropRight > 0)
                    {
                        filterList.Add(Services.LocalizationService.GetString("Tasks_Filter_Crop"));
                        hasFilters = true;
                    }
                }

                // 检查翻转
                var flipH = context.GetData<bool>("ffmpeg-size-layout", "FlipHorizontal");
                var flipV = context.GetData<bool>("ffmpeg-size-layout", "FlipVertical");
                if (flipH)
                {
                    filterList.Add(Services.LocalizationService.GetString("Tasks_Filter_FlipH"));
                    hasFilters = true;
                }
                if (flipV)
                {
                    filterList.Add(Services.LocalizationService.GetString("Tasks_Filter_FlipV"));
                    hasFilters = true;
                }

                // 检查旋转
                var rotationAngle = context.GetData<double>("ffmpeg-size-layout", "RotationAngle");
                if (Math.Abs(rotationAngle) > 0.001)
                {
                    filterList.Add($"{Services.LocalizationService.GetString("Tasks_Filter_Rotate")} ({rotationAngle}°)");
                    hasFilters = true;
                }

                if (hasFilters)
                {
                    AdditionalSettings.Add(new KeyValuePair<string, string>(
                        Services.LocalizationService.GetString("Tasks_Filters"), 
                        string.Join(", ", filterList)));
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Added Filters: {string.Join(", ", filterList)}");
#endif
                }

                // 生成命令预览
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Generating command preview...");
#endif
                var commandBuilder = context.GetCurrentCommandBuilder();
                if (commandBuilder != null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"CommandBuilder type: {commandBuilder.GetType().Name}");
#endif
                    var preview = commandBuilder.GetPreview(context);
                    
                    // 如果预览为空或只有ffmpeg，显示提示信息
                    if (string.IsNullOrWhiteSpace(preview) || preview.Trim() == "ffmpeg")
                    {
                        CommandPreview = $"# {Services.LocalizationService.GetString("Tasks_NoConfiguration")}\n# {Services.LocalizationService.GetString("Tasks_ConfigureInOperations")}";
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Preview is empty or minimal, showing help message");
#endif
                    }
                    else
                    {
                        CommandPreview = preview;
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Command Preview (first 200 chars): {preview.Substring(0, Math.Min(200, preview.Length))}...");
#endif
                    }
                }
                else
                {
                    CommandPreview = $"# {Services.LocalizationService.GetString("Tasks_NoCommandBuilder")}\n# {Services.LocalizationService.GetString("Tasks_ConfigureBackend")}";
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("CommandBuilder is null!");
#endif
                }
            }
            catch (Exception ex)
            {
                CommandPreview = $"# {Services.LocalizationService.GetString("Tasks_PreviewError")}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"RefreshPreview error: {ex}");
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
#endif
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("=== TasksViewModel.RefreshPreview() END ===");
#endif
        }

        private string GetLocalizedBackendName(BackendType backendType)
        {
            return backendType switch
            {
                BackendType.FFmpeg => Services.LocalizationService.GetString("Backend_FFmpeg"),
                BackendType.VapourSynth => Services.LocalizationService.GetString("Backend_VapourSynth"),
                _ => backendType.ToString()
            };
        }

        private string GetEncoderDisplayName(int codecIndex, int encoderIndex)
        {
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
                3 => "H.266 (VVC)",
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
                7 => Services.LocalizationService.GetString("Tasks_Uncompressed"),
                _ => Services.LocalizationService.GetString("Tasks_Unknown")
            };
        }

        private string GetContainerDisplayName(int containerIndex)
        {
            return containerIndex switch
            {
                0 => "MKV",
                1 => "MP4",
                2 => "QuickTime (MOV)",
                3 => "MPEG-TS",
                4 => "FLV",
                _ => Services.LocalizationService.GetString("Tasks_NotSet")
            };
        }

        private string GetPresetLabel(int codecIndex, int encoderIndex, int presetValue)
        {
            return $"{Services.LocalizationService.GetString("Tasks_Preset")} {presetValue}";
        }

        private string GetRateModeLabel(int rateModeIndex)
        {
            return rateModeIndex switch
            {
                0 => "CRF",
                1 => "CBR",
                2 => "VBR",
                3 => "ABR",
                4 => "CQP",
                _ => Services.LocalizationService.GetString("Tasks_NotSet")
            };
        }

        private void ExecuteCopyCommand(object? parameter)
        {
            try
            {
                Clipboard.SetText(CommandPreview);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"复制命令失败: {ex.Message}");
            }
        }

        private bool CanExecuteCopyCommand(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(CommandPreview);
        }

        private void ExecuteRefreshPreview(object? parameter)
        {
            RefreshPreview();
        }

        private void ExecuteAddToQueue(object? parameter)
        {
            try
            {
                var context = OperationContext.Instance;
                var commandBuilder = context.GetCurrentCommandBuilder();
                
                if (commandBuilder == null)
                {
                    Services.NotificationService.SendWarning("Tasks_NoCommandBuilder");
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("AddToQueue: CommandBuilder is null");
#endif
                    return;
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"=== AddToQueue: Processing {MediaItems.Count} media items ===");
#endif

                // 创建CascadeIO服务实例
                var cascadeIOService = new Services.CascadeIO.CascadeIOService();

                // 为所有媒体项生成任务
                var tasksToAdd = new List<Services.QueueTask>();
                
                foreach (var mediaItem in MediaItems)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Processing media: {mediaItem.Name}");
#endif
                    // 设置当前输入文件
                    context.PublishData("cascade-io-input", "currentInputFile", mediaItem.FilePath);
                    
                    // 使用CascadeIO服务解析输出路径
                    var taskItem = cascadeIOService.CreateTask(mediaItem.FilePath, context);
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"TaskItem IsValid: {taskItem.IsValid}");
                    System.Diagnostics.Debug.WriteLine($"Output path: {taskItem.OutputPath.FullPath}");
                    if (taskItem.Errors.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Errors: {string.Join(", ", taskItem.Errors)}");
                    }
#endif

                    if (!taskItem.IsValid)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Skipping {mediaItem.Name}: Invalid task item");
#endif
                        continue;
                    }

                    // 生成命令
                    var commandPreview = commandBuilder.GetPreview(context);
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Command preview (first 200 chars): {commandPreview?.Substring(0, Math.Min(200, commandPreview?.Length ?? 0))}");
#endif

                    if (string.IsNullOrWhiteSpace(commandPreview) || commandPreview.Trim() == "ffmpeg")
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Skipping {mediaItem.Name}: Invalid command preview");
#endif
                        continue;
                    }

                    // 移除格式化（换行符和注释），转换为单行命令
                    var command = commandPreview
                        .Split('\n')
                        .Where(line => !line.TrimStart().StartsWith("#"))
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line))
                        .Aggregate((a, b) => a + " " + b);

                    // 根据文件冲突处理策略决定是否添加 -y 参数
                    var conflictResolution = context.GetData<int>("cascade-io-naming-output", "SelectedConflictResolutionIndex", 0);
                    if (conflictResolution == 5) // ConflictResolution.Overwrite
                    {
                        // 添加 -y 参数以自动覆盖文件
                        command = command.Replace("ffmpeg ", "ffmpeg -y ");
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Added -y flag for overwrite mode");
#endif
                    }

                    // 注意：不要再添加输出文件路径，因为OutputFileSegmentProvider已经添加了
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Final command: {command}");
#endif

                    // 创建队列任务
                    var task = new Services.QueueTask
                    {
                        MediaItem = mediaItem,
                        Command = command,
                        OutputPath = taskItem.OutputPath.FullPath
                    };

                    tasksToAdd.Add(task);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Task added for {mediaItem.Name}");
#endif
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Total tasks to add: {tasksToAdd.Count}");
#endif

                if (tasksToAdd.Count > 0)
                {
                    // 添加到队列服务
                    Services.QueueService.Instance.AddTasks(tasksToAdd);
                    
                    // 发送通知
                    Services.NotificationService.SendInformation(
                        $"{Services.LocalizationService.GetString("Queue_TasksAdded")}: {tasksToAdd.Count}",
                        false);
                }
                else
                {
                    Services.NotificationService.SendWarning("Tasks_NoValidTasks");
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("No valid tasks to add to queue");
#endif
                }
            }
            catch (Exception ex)
            {
                Services.NotificationService.SendWarning($"Tasks_AddToQueueError: {ex.Message}");
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"=== AddToQueue Exception ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");
#endif
            }
        }

        private bool CanExecuteAddToQueue(object? parameter)
        {
            return MediaItems.Count > 0;
        }

        /// <summary>
        /// 更新媒体项列表（从MediaViewModel同步所有媒体）
        /// </summary>
        public void UpdateMediaItems(IEnumerable<MediaItem> mediaItems)
        {
            MediaItems.Clear();
            foreach (var item in mediaItems)
            {
                MediaItems.Add(item);
            }
            HasMedia = MediaItems.Count > 0;
        }
    }
}
