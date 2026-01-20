using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cascade.Models;
using Cascade.Services.CommandBuilders;
using Cascade.ViewModels.Operations;

namespace Cascade.Services
{
    /// <summary>
    /// 队列任务项
    /// </summary>
    public class QueueTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MediaItem MediaItem { get; set; }
        public string Command { get; set; }
        public string OutputPath { get; set; }
        public QueueTaskStatus Status { get; set; } = QueueTaskStatus.Waiting;
        public double Progress { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public double? TotalDurationSeconds { get; set; }
    }

    /// <summary>
    /// 队列任务状态
    /// </summary>
    public enum QueueTaskStatus
    {
        Waiting,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// 队列服务 - 管理和执行FFmpeg任务队列
    /// </summary>
    public class QueueService
    {
        private static QueueService? _instance;
        private static readonly object _lock = new object();

        private readonly ConcurrentQueue<QueueTask> _taskQueue = new ConcurrentQueue<QueueTask>();
        private readonly List<QueueTask> _allTasks = new List<QueueTask>();
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        private bool _isRunning = false;
        private int _maxConcurrentTasks = 4;

        /// <summary>
        /// 获取队列服务单例
        /// </summary>
        public static QueueService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new QueueService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 任务状态变更事件
        /// </summary>
        public event EventHandler<QueueTask>? TaskStatusChanged;

        /// <summary>
        /// 任务进度更新事件
        /// </summary>
        public event EventHandler<QueueTask>? TaskProgressUpdated;

        private QueueService()
        {
            _semaphore = new SemaphoreSlim(_maxConcurrentTasks, _maxConcurrentTasks);
        }

        /// <summary>
        /// 最大并行任务数
        /// </summary>
        public int MaxConcurrentTasks
        {
            get => _maxConcurrentTasks;
            set
            {
                if (value > 0 && value <= 16)
                {
                    _maxConcurrentTasks = value;
                }
            }
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public IReadOnlyList<QueueTask> GetAllTasks()
        {
            lock (_allTasks)
            {
                return _allTasks.ToList();
            }
        }

        /// <summary>
        /// 添加任务到队列
        /// </summary>
        public void AddTask(QueueTask task)
        {
            lock (_allTasks)
            {
                _allTasks.Add(task);
            }
            _taskQueue.Enqueue(task);
            TaskStatusChanged?.Invoke(this, task);
        }

        /// <summary>
        /// 批量添加任务
        /// </summary>
        public void AddTasks(IEnumerable<QueueTask> tasks)
        {
            foreach (var task in tasks)
            {
                AddTask(task);
            }
        }

        /// <summary>
        /// 移除任务
        /// </summary>
        public bool RemoveTask(string taskId)
        {
            lock (_allTasks)
            {
                var task = _allTasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null && task.Status == QueueTaskStatus.Waiting)
                {
                    task.Status = QueueTaskStatus.Cancelled;
                    _allTasks.Remove(task);
                    TaskStatusChanged?.Invoke(this, task);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 批量移除任务
        /// </summary>
        public int RemoveTasks(IEnumerable<string> taskIds)
        {
            var removedCount = 0;
            foreach (var taskId in taskIds)
            {
                if (RemoveTask(taskId))
                {
                    removedCount++;
                }
            }
            return removedCount;
        }

        /// <summary>
        /// 上移任务
        /// </summary>
        public bool MoveTaskUp(string taskId)
        {
            lock (_allTasks)
            {
                var task = _allTasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null || task.Status != QueueTaskStatus.Waiting)
                    return false;

                var index = _allTasks.IndexOf(task);
                if (index <= 0)
                    return false;

                // 只能在等待任务中移动
                var previousTask = _allTasks[index - 1];
                if (previousTask.Status != QueueTaskStatus.Waiting)
                    return false;

                _allTasks.RemoveAt(index);
                _allTasks.Insert(index - 1, task);
                
                TaskStatusChanged?.Invoke(this, task);
                return true;
            }
        }

        /// <summary>
        /// 下移任务
        /// </summary>
        public bool MoveTaskDown(string taskId)
        {
            lock (_allTasks)
            {
                var task = _allTasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null || task.Status != QueueTaskStatus.Waiting)
                    return false;

                var index = _allTasks.IndexOf(task);
                if (index < 0 || index >= _allTasks.Count - 1)
                    return false;

                // 只能在等待任务中移动
                var nextTask = _allTasks[index + 1];
                if (nextTask.Status != QueueTaskStatus.Waiting)
                    return false;

                _allTasks.RemoveAt(index);
                _allTasks.Insert(index + 1, task);
                
                TaskStatusChanged?.Invoke(this, task);
                return true;
            }
        }

        /// <summary>
        /// 批量上移任务
        /// </summary>
        public int MoveTasksUp(IEnumerable<string> taskIds)
        {
            var movedCount = 0;
            // 从上到下移动，避免顺序混乱
            var orderedIds = taskIds.ToList();
            foreach (var taskId in orderedIds)
            {
                if (MoveTaskUp(taskId))
                {
                    movedCount++;
                }
            }
            return movedCount;
        }

        /// <summary>
        /// 批量下移任务
        /// </summary>
        public int MoveTasksDown(IEnumerable<string> taskIds)
        {
            var movedCount = 0;
            // 从下到上移动，避免顺序混乱
            var orderedIds = taskIds.Reverse().ToList();
            foreach (var taskId in orderedIds)
            {
                if (MoveTaskDown(taskId))
                {
                    movedCount++;
                }
            }
            return movedCount;
        }

        /// <summary>
        /// 清除已完成的任务
        /// </summary>
        public void ClearCompletedTasks()
        {
            lock (_allTasks)
            {
                _allTasks.RemoveAll(t => t.Status == QueueTaskStatus.Completed);
            }
        }

        /// <summary>
        /// 启动队列处理
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
            
            NotificationService.SendInformation("Queue_Started", true);
        }

        /// <summary>
        /// 暂停队列处理
        /// </summary>
        public void Pause()
        {
            _isRunning = false;
            NotificationService.SendInformation("Queue_Paused", true);
        }

        /// <summary>
        /// 处理队列
        /// </summary>
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_isRunning)
                {
                    await Task.Delay(500, cancellationToken);
                    continue;
                }

                if (_taskQueue.TryDequeue(out var task))
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    
                    var processTask = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessTaskAsync(task, cancellationToken);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, cancellationToken);

                    tasks.Add(processTask);
                }
                else
                {
                    // 队列为空，等待一段时间
                    await Task.Delay(500, cancellationToken);
                }

                // 清理已完成的任务
                tasks.RemoveAll(t => t.IsCompleted);
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 处理单个任务
        /// </summary>
        private async Task ProcessTaskAsync(QueueTask task, CancellationToken cancellationToken)
        {
            try
            {
                task.Status = QueueTaskStatus.Processing;
                task.StartTime = DateTime.Now;
                TaskStatusChanged?.Invoke(this, task);

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"=== Processing Task: {task.MediaItem.Name} ===");
                System.Diagnostics.Debug.WriteLine($"Task ID: {task.Id}");
                System.Diagnostics.Debug.WriteLine($"Command: {task.Command}");
                System.Diagnostics.Debug.WriteLine($"Output Path: {task.OutputPath}");
#endif

                // 执行FFmpeg命令
                var success = await ExecuteFFmpegCommandAsync(task, cancellationToken);

                if (success)
                {
                    task.Status = QueueTaskStatus.Completed;
                    task.Progress = 100;
                    task.EndTime = DateTime.Now;
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Task completed successfully: {task.MediaItem.Name}");
                    System.Diagnostics.Debug.WriteLine($"Duration: {task.EndTime - task.StartTime}");
#endif
                    
                    // 发送成功通知
                    NotificationService.SendInformation(
                        $"{LocalizationService.GetString("Queue_TaskCompleted")}: {task.MediaItem.Name}",
                        false);
                }
                else
                {
                    task.Status = QueueTaskStatus.Failed;
                    task.EndTime = DateTime.Now;
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"=== TASK FAILED: {task.MediaItem.Name} ===");
                    System.Diagnostics.Debug.WriteLine($"Task ID: {task.Id}");
                    System.Diagnostics.Debug.WriteLine($"Error Message: {task.ErrorMessage}");
                    System.Diagnostics.Debug.WriteLine($"Command: {task.Command}");
                    System.Diagnostics.Debug.WriteLine($"Output Path: {task.OutputPath}");
                    System.Diagnostics.Debug.WriteLine($"Duration: {task.EndTime - task.StartTime}");
                    System.Diagnostics.Debug.WriteLine("===========================================");
#endif
                    
                    // 发送失败警告
                    NotificationService.SendWarning(
                        $"{LocalizationService.GetString("Queue_TaskFailed")}: {task.MediaItem.Name}",
                        false);
                }

                TaskStatusChanged?.Invoke(this, task);
            }
            catch (Exception ex)
            {
                task.Status = QueueTaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.EndTime = DateTime.Now;
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"=== TASK EXCEPTION: {task.MediaItem.Name} ===");
                System.Diagnostics.Debug.WriteLine($"Task ID: {task.Id}");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Exception Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Command: {task.Command}");
                System.Diagnostics.Debug.WriteLine($"Output Path: {task.OutputPath}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace:\n{ex.InnerException.StackTrace}");
                }
                System.Diagnostics.Debug.WriteLine("===========================================");
#endif
                
                TaskStatusChanged?.Invoke(this, task);
                
                NotificationService.SendWarning(
                    $"{LocalizationService.GetString("Queue_TaskFailed")}: {task.MediaItem.Name} - {ex.Message}",
                    false);
            }
        }

        /// <summary>
        /// 执行FFmpeg命令
        /// </summary>
        private async Task<bool> ExecuteFFmpegCommandAsync(QueueTask task, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Starting FFmpeg execution for: {task.MediaItem.Name}");
                    System.Diagnostics.Debug.WriteLine($"Command to execute: {task.Command}");
#endif

                    // 确保输出目录存在
                    var outputDirectory = System.IO.Path.GetDirectoryName(task.OutputPath);
                    if (!string.IsNullOrEmpty(outputDirectory) && !System.IO.Directory.Exists(outputDirectory))
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Creating output directory: {outputDirectory}");
#endif
                        System.IO.Directory.CreateDirectory(outputDirectory);
                    }

                    // 解析命令：提取ffmpeg和参数
                    var command = task.Command.Trim();
                    
                    // 移除"ffmpeg"前缀，获取参数
                    var arguments = command.StartsWith("ffmpeg ", StringComparison.OrdinalIgnoreCase) 
                        ? command.Substring(7).Trim() 
                        : command;
                    
                    // 确保包含 -y 或 -n 参数以避免交互式提示
                    if (!arguments.Contains(" -y ") && !arguments.Contains(" -n ") && !arguments.StartsWith("-y ") && !arguments.StartsWith("-n "))
                    {
                        arguments = "-y " + arguments;
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Added -y flag to avoid interactive prompt");
#endif
                    }

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"FFmpeg arguments: {arguments}");
#endif

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    };

                    using var process = new Process { StartInfo = processStartInfo };
                    
                    var outputBuilder = new System.Text.StringBuilder();
                    var errorBuilder = new System.Text.StringBuilder();
                    
                    // 监听输出以更新进度
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
#if DEBUG
                            outputBuilder.AppendLine(e.Data);
                            System.Diagnostics.Debug.WriteLine($"[STDOUT] {e.Data}");
#endif
                        }
                    };
                    
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
#if DEBUG
                            errorBuilder.AppendLine(e.Data);
                            System.Diagnostics.Debug.WriteLine($"[STDERR] {e.Data}");
#endif
                            // 解析FFmpeg输出以获取进度
                            var progress = ParseFFmpegProgress(e.Data, task);
                            if (progress >= 0)
                            {
                                task.Progress = progress;
                                TaskProgressUpdated?.Invoke(this, task);
                            }
                        }
                    };

                    process.Start();
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Process started (PID: {process.Id})");
#endif
                    
                    // 必须在Start之后立即开始异步读取，否则可能死锁
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    
                    // 关闭标准输入以防止进程等待输入
                    try
                    {
                        process.StandardInput.Close();
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("StandardInput closed");
#endif
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Failed to close StandardInput: {ex.Message}");
#endif
                    }
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Waiting for process to exit...");
#endif
                    
                    // 使用WaitForExit with timeout
                    var timeout = (int)TimeSpan.FromMinutes(60).TotalMilliseconds;
                    if (!process.WaitForExit(timeout))
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Process timeout! Killing process...");
#endif
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        task.ErrorMessage = "Process timeout after 60 minutes";
                        return false;
                    }
                    
                    // 确保所有输出都被读取完毕
                    process.WaitForExit();
                    
                    var exitCode = process.ExitCode;
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"FFmpeg process exited with code: {exitCode}");
                    
                    if (exitCode != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"=== FFmpeg STDOUT for {task.MediaItem.Name} ===");
                        System.Diagnostics.Debug.WriteLine(outputBuilder.ToString());
                        System.Diagnostics.Debug.WriteLine($"=== FFmpeg STDERR for {task.MediaItem.Name} ===");
                        System.Diagnostics.Debug.WriteLine(errorBuilder.ToString());
                        System.Diagnostics.Debug.WriteLine("===========================================");
                        
                        // 保存错误信息到任务
                        task.ErrorMessage = $"Exit code: {exitCode}. Check debug output for details.";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Task completed successfully");
                    }
#endif
                    
                    return exitCode == 0;
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"=== FFmpeg Execution Exception for {task.MediaItem.Name} ===");
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                    System.Diagnostics.Debug.WriteLine("===========================================");
#endif
                    task.ErrorMessage = ex.Message;
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 解析FFmpeg输出以获取进度
        /// </summary>
        private double ParseFFmpegProgress(string output, QueueTask task)
        {
            try
            {
                // 首先尝试获取视频总时长（从Duration行）
                // Duration: 00:01:23.45, start: 0.000000, bitrate: 1234 kb/s
                if (output.Contains("Duration:") && !output.Contains("N/A"))
                {
                    var durationMatch = System.Text.RegularExpressions.Regex.Match(
                        output, 
                        @"Duration:\s*(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                    
                    if (durationMatch.Success)
                    {
                        var hours = int.Parse(durationMatch.Groups[1].Value);
                        var minutes = int.Parse(durationMatch.Groups[2].Value);
                        var seconds = int.Parse(durationMatch.Groups[3].Value);
                        var centiseconds = int.Parse(durationMatch.Groups[4].Value);
                        
                        task.TotalDurationSeconds = hours * 3600 + minutes * 60 + seconds + centiseconds / 100.0;
                        
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Detected total duration: {task.TotalDurationSeconds:F2} seconds");
#endif
                    }
                }
                
                // 解析当前编码时间
                // frame= 1234 fps= 30 q=28.0 size=   12345kB time=00:01:23.45 bitrate=1234.5kbits/s speed=1.23x
                if (output.Contains("time=") && task.TotalDurationSeconds.HasValue && task.TotalDurationSeconds.Value > 0)
                {
                    var timeMatch = System.Text.RegularExpressions.Regex.Match(
                        output, 
                        @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                    
                    if (timeMatch.Success)
                    {
                        var hours = int.Parse(timeMatch.Groups[1].Value);
                        var minutes = int.Parse(timeMatch.Groups[2].Value);
                        var seconds = int.Parse(timeMatch.Groups[3].Value);
                        var centiseconds = int.Parse(timeMatch.Groups[4].Value);
                        
                        var currentSeconds = hours * 3600 + minutes * 60 + seconds + centiseconds / 100.0;
                        var progress = (currentSeconds / task.TotalDurationSeconds.Value) * 100.0;
                        
                        // 限制在0-100范围内
                        progress = Math.Max(0, Math.Min(100, progress));
                        
                        // 计算预估剩余时间
                        if (progress > 0 && task.StartTime.HasValue)
                        {
                            var elapsed = DateTime.Now - task.StartTime.Value;
                            var estimatedTotal = TimeSpan.FromSeconds(elapsed.TotalSeconds / progress * 100);
                            task.EstimatedTimeRemaining = estimatedTotal - elapsed;
                            
                            // 确保不为负数
                            if (task.EstimatedTimeRemaining.Value.TotalSeconds < 0)
                            {
                                task.EstimatedTimeRemaining = TimeSpan.Zero;
                            }
                        }
                        
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Progress: {progress:F1}% (current: {currentSeconds:F2}s / total: {task.TotalDurationSeconds:F2}s)");
                        if (task.EstimatedTimeRemaining.HasValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"Estimated time remaining: {task.EstimatedTimeRemaining.Value:hh\\:mm\\:ss}");
                        }
#endif
                        
                        return progress;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Error parsing FFmpeg progress: {ex.Message}");
#endif
            }
            
            return -1;
        }
    }
}
