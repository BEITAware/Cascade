using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Cascade.Models.Operations;
using Cascade.Services;
using Cascade.ViewModels.Operations;

namespace Cascade.ViewModels
{
    /// <summary>
    /// 预设管理窗口的ViewModel
    /// </summary>
    public class PresetManagementViewModel : ViewModelBase
    {
        private string _newPresetName = string.Empty;
        private string? _selectedPreset;
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 预设列表
        /// </summary>
        public ObservableCollection<string> Presets { get; }

        /// <summary>
        /// 新预设名称
        /// </summary>
        public string NewPresetName
        {
            get => _newPresetName;
            set => SetProperty(ref _newPresetName, value);
        }

        /// <summary>
        /// 选中的预设
        /// </summary>
        public string? SelectedPreset
        {
            get => _selectedPreset;
            set => SetProperty(ref _selectedPreset, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 保存当前配置为预设命令
        /// </summary>
        public ICommand SaveCurrentAsPresetCommand { get; }

        /// <summary>
        /// 加载预设命令
        /// </summary>
        public ICommand LoadPresetCommand { get; }

        /// <summary>
        /// 删除预设命令
        /// </summary>
        public ICommand DeletePresetCommand { get; }

        /// <summary>
        /// 刷新预设列表命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        public PresetManagementViewModel()
        {
            Presets = new ObservableCollection<string>();
            
            SaveCurrentAsPresetCommand = new RelayCommand(SaveCurrentAsPreset, CanSaveCurrentAsPreset);
            LoadPresetCommand = new RelayCommand(LoadPreset, CanLoadPreset);
            DeletePresetCommand = new RelayCommand(DeletePreset, CanDeletePreset);
            RefreshCommand = new RelayCommand(RefreshPresets);

            RefreshPresets();
        }

        private void SaveCurrentAsPreset()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewPresetName))
                {
                    StatusMessage = LocalizationService.GetString("PresetManagement_EmptyName");
                    return;
                }

                var context = OperationContext.Instance;
                
                // 调试信息：显示要保存的数据
                var state = context.ExportState();
                var sharedDataCount = state.ContainsKey("SharedData") && state["SharedData"] is Dictionary<string, Dictionary<string, object>> sd 
                    ? sd.Count 
                    : 0;
                
                PresetManager.Instance.SavePreset(NewPresetName, context);
                
                StatusMessage = string.Format(
                    LocalizationService.GetString("PresetManagement_SaveSuccess"), 
                    NewPresetName) + $" (共享数据页面数: {sharedDataCount})";
                
                NewPresetName = string.Empty;
                RefreshPresets();
            }
            catch (Exception ex)
            {
                StatusMessage = $"{LocalizationService.GetString("PresetManagement_SaveFailed")}: {ex.Message}";
            }
        }

        private bool CanSaveCurrentAsPreset()
        {
            return !string.IsNullOrWhiteSpace(NewPresetName);
        }

        private void LoadPreset()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedPreset))
                {
                    return;
                }

                var context = OperationContext.Instance;
                
                // 保存加载前的状态用于调试
                var beforeState = context.ExportState();
                var beforeSharedDataCount = beforeState.ContainsKey("SharedData") && beforeState["SharedData"] is Dictionary<string, Dictionary<string, object>> sd1 
                    ? sd1.Count 
                    : 0;
                
                PresetManager.Instance.LoadPreset(SelectedPreset, context);
                
                // 检查加载后的状态
                var afterState = context.ExportState();
                var afterSharedDataCount = afterState.ContainsKey("SharedData") && afterState["SharedData"] is Dictionary<string, Dictionary<string, object>> sd2 
                    ? sd2.Count 
                    : 0;
                
                StatusMessage = string.Format(
                    LocalizationService.GetString("PresetManagement_LoadSuccess"), 
                    SelectedPreset) + $" (加载前: {beforeSharedDataCount}页, 加载后: {afterSharedDataCount}页)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"{LocalizationService.GetString("PresetManagement_LoadFailed")}: {ex.Message}";
            }
        }

        private bool CanLoadPreset()
        {
            return !string.IsNullOrEmpty(SelectedPreset);
        }

        private void DeletePreset()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedPreset))
                {
                    return;
                }

                PresetManager.Instance.DeletePreset(SelectedPreset);
                
                StatusMessage = string.Format(
                    LocalizationService.GetString("PresetManagement_DeleteSuccess"), 
                    SelectedPreset);
                
                SelectedPreset = null;
                RefreshPresets();
            }
            catch (Exception ex)
            {
                StatusMessage = $"{LocalizationService.GetString("PresetManagement_DeleteFailed")}: {ex.Message}";
            }
        }

        private bool CanDeletePreset()
        {
            return !string.IsNullOrEmpty(SelectedPreset);
        }

        private void RefreshPresets()
        {
            Presets.Clear();
            var presets = PresetManager.Instance.ListPresets();
            foreach (var preset in presets)
            {
                Presets.Add(preset);
            }
            
            StatusMessage = string.Format(
                LocalizationService.GetString("PresetManagement_PresetsCount"), 
                Presets.Count);
        }
    }
}
