using Cascade.Models.Operations;

namespace Cascade.ViewModels.Operations
{
    /// <summary>
    /// 可选择的子页面ViewModel包装器
    /// 将Model（OperationSubPageInfo）与UI状态（IsSelected）分离
    /// </summary>
    public class SelectableSubPageViewModel : ViewModelBase
    {
        private bool _isSelected;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="subPageInfo">子页面信息Model</param>
        public SelectableSubPageViewModel(OperationSubPageInfo subPageInfo)
        {
            SubPageInfo = subPageInfo;
        }

        /// <summary>
        /// 子页面信息Model（只读）
        /// </summary>
        public OperationSubPageInfo SubPageInfo { get; }

        /// <summary>
        /// 是否被选中（UI状态）
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 便捷属性：子页面ID
        /// </summary>
        public string Id => SubPageInfo.Id;

        /// <summary>
        /// 便捷属性：显示名称
        /// </summary>
        public string DisplayName => SubPageInfo.DisplayName;
    }
}
