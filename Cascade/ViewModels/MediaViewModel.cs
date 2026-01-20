using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cascade.Helpers;
using Cascade.Models;
using Microsoft.Win32;

namespace Cascade.ViewModels
{
    public class MediaViewModel : ViewModelBase
    {
        private MediaItem? _selectedMediaItem;

        /// <summary>
        /// 媒体项目集合
        /// </summary>
        public ObservableCollection<MediaItem> MediaItems { get; } = new ObservableCollection<MediaItem>();

        /// <summary>
        /// 当前选中的媒体项目集合
        /// </summary>
        public ObservableCollection<MediaItem> SelectedMediaItems { get; } = new ObservableCollection<MediaItem>();

        /// <summary>
        /// 当前选中的媒体项目（用于显示详情，通常是最后选中的那个）
        /// </summary>
        public MediaItem? SelectedMediaItem
        {
            get => _selectedMediaItem;
            set
            {
                if (SetProperty(ref _selectedMediaItem, value))
                {
                    // 当选中的媒体项改变时，更新播放器的当前媒体
                    PlayerViewModel.CurrentMediaItem = value;
                }
            }
        }

        /// <summary>
        /// 播放器视图模型
        /// </summary>
        public PlayerViewModel PlayerViewModel { get; } = new PlayerViewModel();

        // 命令
        public ICommand AddFileCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearCommand { get; }

        public MediaViewModel()
        {
            AddFileCommand = new RelayCommand(ExecuteAddFile);
            AddFolderCommand = new RelayCommand(ExecuteAddFolder);
            RemoveCommand = new RelayCommand(ExecuteRemove, CanExecuteRemove);
            ClearCommand = new RelayCommand(ExecuteClear, CanExecuteClear);
        }

        private void ExecuteAddFile(object? parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "媒体文件|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.mp3;*.wav|所有文件|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AddFiles(openFileDialog.FileNames);
            }
        }

        private void ExecuteAddFolder(object? parameter)
        {
            // WPF 原生没有 FolderBrowserDialog，这里暂时使用 OpenFileDialog 模拟或需要引入 WinForms/第三方库
            // 为了保持纯净，这里暂时留空或使用简单的逻辑，实际项目中通常使用 Ookii.Dialogs 或 WinForms 引用
            // 这里仅作为占位符，实际实现可能需要 System.Windows.Forms
            
            // 简单模拟：让用户选择文件夹内的一个文件，然后添加该文件夹下的所有媒体文件
            var openFileDialog = new OpenFileDialog
            {
                Title = "请选择文件夹内的一个文件以添加整个文件夹",
                Filter = "所有文件|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string? folderPath = Path.GetDirectoryName(openFileDialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    var files = Directory.GetFiles(folderPath, "*.*")
                        .Where(s => s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || 
                                    s.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                                    s.EndsWith(".avi", StringComparison.OrdinalIgnoreCase));
                    AddFiles(files.ToArray());
                }
            }
        }

        private void ExecuteRemove(object? parameter)
        {
            // 创建一个副本以避免在遍历时修改集合
            var itemsToRemove = SelectedMediaItems.ToList();
            
            foreach (var item in itemsToRemove)
            {
                MediaItems.Remove(item);
                SelectedMediaItems.Remove(item);
            }

            // 如果还有选中的项目（理论上不应该，除非删除失败），更新 SelectedMediaItem
            // 否则清空 SelectedMediaItem
            if (SelectedMediaItems.Count == 0)
            {
                SelectedMediaItem = null;
            }
            else
            {
                SelectedMediaItem = SelectedMediaItems.LastOrDefault();
            }
        }

        private bool CanExecuteRemove(object? parameter)
        {
            return SelectedMediaItems.Count > 0;
        }

        private void ExecuteClear(object? parameter)
        {
            MediaItems.Clear();
            SelectedMediaItems.Clear();
            SelectedMediaItem = null;
        }

        private bool CanExecuteClear(object? parameter)
        {
            return MediaItems.Count > 0;
        }

        /// <summary>
        /// 添加文件到集合
        /// </summary>
        /// <param name="filePaths">文件路径数组</param>
        public void AddFiles(string[] filePaths)
        {
            foreach (var path in filePaths)
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    var newItem = new MediaItem
                    {
                        Name = fileInfo.Name,
                        Format = fileInfo.Extension.TrimStart('.').ToUpper(),
                        Size = FormatSize(fileInfo.Length),
                        Duration = "...", // 正在加载
                        Resolution = "...", // 正在加载
                        FilePath = path
                    };
                    
                    MediaItems.Add(newItem);

                    // 异步加载媒体信息
                    _ = Task.Run(() => MediaHelper.LoadMediaInfoAsync(newItem));
                }
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
