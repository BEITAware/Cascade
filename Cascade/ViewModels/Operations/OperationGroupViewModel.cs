using System.Collections.ObjectModel;
using System.Linq;
using Cascade.Models.Operations;

namespace Cascade.ViewModels.Operations
{
    /// <summary>
    /// 操作分组ViewModel包装器
    /// 将Model（OperationGroup）包装为ViewModel，子页面使用SelectableSubPageViewModel
    /// </summary>
    public class OperationGroupViewModel : ViewModelBase
    {
        private bool _isExpanded;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="group">分组Model</param>
        public OperationGroupViewModel(OperationGroup group)
        {
            Group = group;
            _isExpanded = group.IsExpanded;

            // 将子页面包装为SelectableSubPageViewModel
            SubPages = new ObservableCollection<SelectableSubPageViewModel>(
                group.SubPages.Select(sp => new SelectableSubPageViewModel(sp))
            );

            // 监听Model的SubPages变化，同步到ViewModel
            group.SubPages.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (OperationSubPageInfo item in e.NewItems)
                    {
                        SubPages.Add(new SelectableSubPageViewModel(item));
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (OperationSubPageInfo item in e.OldItems)
                    {
                        var vm = SubPages.FirstOrDefault(sp => sp.SubPageInfo == item);
                        if (vm != null)
                        {
                            SubPages.Remove(vm);
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 分组Model（只读）
        /// </summary>
        public OperationGroup Group { get; }

        /// <summary>
        /// 子页面ViewModel集合
        /// </summary>
        public ObservableCollection<SelectableSubPageViewModel> SubPages { get; }

        /// <summary>
        /// 分组ID
        /// </summary>
        public string Id => Group.Id;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => Group.DisplayName;

        /// <summary>
        /// 是否展开
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value))
                {
                    Group.IsExpanded = value; // 同步到Model
                }
            }
        }
    }
}
