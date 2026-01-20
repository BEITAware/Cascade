using System.Text.Json.Nodes;
using System.Windows.Input;
using Cascade.ViewModels.Operations;
using Cascade.Services;

namespace Cascade.ViewModels.Operations.CascadeIO
{
    /// <summary>
    /// 命名与输出子页面ViewModel
    /// </summary>
    public class NamingAndOutputViewModel : OperationViewModelBase
    {
        public override string SubPageId => "cascade-io-naming-output";

        #region 输出模式

        private int _selectedOutputModeIndex;
        public int SelectedOutputModeIndex
        {
            get => _selectedOutputModeIndex;
            set
            {
                if (SetProperty(ref _selectedOutputModeIndex, value))
                {
                    OnPropertyChanged(nameof(ShowOutputPath));
                    OnPropertyChanged(nameof(ShowSubdirectoryName));
                }
            }
        }

        /// <summary>
        /// 是否显示输出路径控件（输出到指定目录 或 克隆源文件夹结构）
        /// </summary>
        public bool ShowOutputPath => SelectedOutputModeIndex == 0 || SelectedOutputModeIndex == 3;

        /// <summary>
        /// 是否显示子目录名称控件（输出到源文件夹子目录）
        /// </summary>
        public bool ShowSubdirectoryName => SelectedOutputModeIndex == 2;

        #endregion

        #region 输出路径

        private string _outputPath = string.Empty;
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        private string _subdirectoryName = "output";
        public string SubdirectoryName
        {
            get => _subdirectoryName;
            set => SetProperty(ref _subdirectoryName, value);
        }

        public ICommand BrowseOutputPathCommand { get; }

        #endregion

        #region 输出名称

        private string _outputFileName = string.Empty;
        public string OutputFileName
        {
            get => _outputFileName;
            set => SetProperty(ref _outputFileName, value);
        }

        #endregion

        #region 文件名冲突处理

        private int _selectedConflictResolutionIndex;
        public int SelectedConflictResolutionIndex
        {
            get => _selectedConflictResolutionIndex;
            set
            {
                if (SetProperty(ref _selectedConflictResolutionIndex, value))
                {
                    OnPropertyChanged(nameof(ShowCustomSuffix));
                }
            }
        }

        /// <summary>
        /// 是否显示自定义后缀输入框（添加自定义后缀）
        /// </summary>
        public bool ShowCustomSuffix => SelectedConflictResolutionIndex == 4;

        private string _customSuffix = "_copy";
        public string CustomSuffix
        {
            get => _customSuffix;
            set => SetProperty(ref _customSuffix, value);
        }

        #endregion

        public NamingAndOutputViewModel()
        {
            BrowseOutputPathCommand = new Cascade.ViewModels.RelayCommand(_ => ExecuteBrowseOutputPath());

            LocalizationService.LanguageChanged += OnLanguageChanged;
            
            // 发布初始值到OperationContext
            PublishInitialValues();
        }

        private void PublishInitialValues()
        {
            PublishData(nameof(SelectedOutputModeIndex), SelectedOutputModeIndex);
            PublishData(nameof(OutputPath), OutputPath);
            PublishData(nameof(SubdirectoryName), SubdirectoryName);
            PublishData(nameof(OutputFileName), OutputFileName);
            PublishData(nameof(SelectedConflictResolutionIndex), SelectedConflictResolutionIndex);
            PublishData(nameof(CustomSuffix), CustomSuffix);
        }

        private void OnLanguageChanged(object? sender, string e)
        {
            // 触发相关属性更新
        }

        private void ExecuteBrowseOutputPath()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = LocalizationService.GetString("NamingOutput_BrowseTitle")
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FolderName;
            }
        }

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["selectedOutputModeIndex"] = SelectedOutputModeIndex,
                ["outputPath"] = OutputPath,
                ["subdirectoryName"] = SubdirectoryName,
                ["outputFileName"] = OutputFileName,
                ["selectedConflictResolutionIndex"] = SelectedConflictResolutionIndex,
                ["customSuffix"] = CustomSuffix
            };
        }

        public override void Deserialize(JsonObject data)
        {
            if (data.TryGetPropertyValue("selectedOutputModeIndex", out var somi))
                SelectedOutputModeIndex = somi?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("outputPath", out var op))
                OutputPath = op?.GetValue<string>() ?? string.Empty;
            if (data.TryGetPropertyValue("subdirectoryName", out var sdn))
                SubdirectoryName = sdn?.GetValue<string>() ?? "output";
            if (data.TryGetPropertyValue("outputFileName", out var ofn))
                OutputFileName = ofn?.GetValue<string>() ?? string.Empty;
            if (data.TryGetPropertyValue("selectedConflictResolutionIndex", out var scri))
                SelectedConflictResolutionIndex = scri?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("customSuffix", out var cs))
                CustomSuffix = cs?.GetValue<string>() ?? "_copy";
        }
    }
}
