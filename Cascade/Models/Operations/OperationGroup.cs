using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cascade.Services;

namespace Cascade.Models.Operations
{
    /// <summary>
    /// 操作分组模型，代表一个功能库分组
    /// </summary>
    public class OperationGroup : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _displayNameKey = string.Empty;
        private bool _isExpanded = true;

        /// <summary>
        /// 分组唯一标识符
        /// </summary>
        public string Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// 本地化资源键，用于获取本地化的显示名称
        /// </summary>
        public string DisplayNameKey
        {
            get => _displayNameKey;
            set { if (_displayNameKey != value) { _displayNameKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } }
        }

        /// <summary>
        /// 分组显示名称（从本地化资源获取）
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(DisplayNameKey) 
            ? Id 
            : LocalizationService.GetString(DisplayNameKey);

        /// <summary>
        /// 分组下的子页面列表
        /// </summary>
        public ObservableCollection<OperationSubPageInfo> SubPages { get; } = new ObservableCollection<OperationSubPageInfo>();

        /// <summary>
        /// 分组是否展开
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
