using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Cascade.Models.Operations;
using Cascade.ViewModels.Operations;
using Cascade.Views;

namespace Cascade.ViewModels
{
    /// <summary>
    /// 操作页面主ViewModel，管理分组显示和子页面切换
    /// </summary>
    public class OperationsViewModel : ViewModelBase
    {
        private readonly Dictionary<string, UserControl> _viewCache = new Dictionary<string, UserControl>();
        private SelectableSubPageViewModel? _selectedSubPageViewModel;
        private UserControl? _currentView;
        private string? _currentSubPageName;

        /// <summary>
        /// 所有分组的ViewModel集合
        /// </summary>
        public ObservableCollection<OperationGroupViewModel> Groups { get; }

        /// <summary>
        /// 打开预设管理窗口命令
        /// </summary>
        public ICommand OpenPresetManagementCommand { get; }

        /// <summary>
        /// 添加为预设命令
        /// </summary>
        public ICommand AddAsPresetCommand { get; }

        /// <summary>
        /// 当前选中的子页面ViewModel
        /// </summary>
        public SelectableSubPageViewModel? SelectedSubPageViewModel
        {
            get => _selectedSubPageViewModel;
            set
            {
                if (SetProperty(ref _selectedSubPageViewModel, value))
                {
                    // 更新所有子页面的IsSelected状态
                    foreach (var group in Groups)
                    {
                        foreach (var subPageVm in group.SubPages)
                        {
                            subPageVm.IsSelected = ReferenceEquals(subPageVm, value);
                        }
                    }

                    if (value != null)
                    {
                        SelectSubPage(value.Id);
                    }
                    else
                    {
                        CurrentView = null;
                        CurrentSubPageName = null;
                    }
                }
            }
        }

        /// <summary>
        /// 当前显示的视图
        /// </summary>
        public UserControl? CurrentView
        {
            get => _currentView;
            private set => SetProperty(ref _currentView, value);
        }

        /// <summary>
        /// 当前子页面名称
        /// </summary>
        public string? CurrentSubPageName
        {
            get => _currentSubPageName;
            private set => SetProperty(ref _currentSubPageName, value);
        }

        public OperationsViewModel()
        {
            // 将Model的Groups包装为ViewModel
            var modelGroups = OperationRegistry.Instance.Groups;
            Groups = new ObservableCollection<OperationGroupViewModel>(
                modelGroups.Select(g => new OperationGroupViewModel(g))
            );

            // 监听Model的Groups变化，同步到ViewModel
            modelGroups.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (OperationGroup item in e.NewItems)
                    {
                        Groups.Add(new OperationGroupViewModel(item));
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (OperationGroup item in e.OldItems)
                    {
                        var vm = Groups.FirstOrDefault(g => g.Group == item);
                        if (vm != null)
                        {
                            Groups.Remove(vm);
                        }
                    }
                }
            };

            // 初始化命令
            OpenPresetManagementCommand = new RelayCommand(OpenPresetManagement);
            AddAsPresetCommand = new RelayCommand(AddAsPreset);
        }

        /// <summary>
        /// 打开预设管理窗口
        /// </summary>
        private void OpenPresetManagement()
        {
            var window = new PresetManagementWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        /// <summary>
        /// 添加当前配置为预设
        /// </summary>
        private void AddAsPreset()
        {
            var window = new PresetManagementWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        /// <summary>
        /// 选择子页面，实现延迟加载逻辑
        /// </summary>
        /// <param name="subPageId">子页面ID</param>
        public void SelectSubPage(string subPageId)
        {
            var subPageInfo = OperationRegistry.Instance.GetSubPageInfo(subPageId);
            if (subPageInfo == null)
            {
                return;
            }

            // 更新当前子页面名称
            CurrentSubPageName = subPageInfo.DisplayName;

            // 检查缓存中是否已有该视图
            if (_viewCache.TryGetValue(subPageId, out var cachedView))
            {
                CurrentView = cachedView;
                return;
            }

            // 延迟加载：创建新的View和ViewModel
            var view = CreateView(subPageInfo);
            if (view != null)
            {
                _viewCache[subPageId] = view;
                CurrentView = view;
            }
        }

        /// <summary>
        /// 根据子页面信息创建View和ViewModel
        /// </summary>
        /// <param name="subPageInfo">子页面信息</param>
        /// <returns>创建的UserControl，如果创建失败则返回null</returns>
        private UserControl? CreateView(OperationSubPageInfo subPageInfo)
        {
            if (subPageInfo.ViewType == null)
            {
                return null;
            }

            try
            {
                // 创建View实例
                var view = Activator.CreateInstance(subPageInfo.ViewType) as UserControl;
                if (view == null)
                {
                    return null;
                }

                // 如果有ViewModel类型，创建并设置DataContext
                if (subPageInfo.ViewModelType != null)
                {
                    var viewModel = Activator.CreateInstance(subPageInfo.ViewModelType);
                    view.DataContext = viewModel;
                }

                return view;
            }
            catch (Exception)
            {
                // 创建失败时返回null，可以在这里添加日志记录
                return null;
            }
        }

        /// <summary>
        /// 检查视图是否已缓存（用于测试）
        /// </summary>
        /// <param name="subPageId">子页面ID</param>
        /// <returns>是否已缓存</returns>
        internal bool IsViewCached(string subPageId)
        {
            return _viewCache.ContainsKey(subPageId);
        }

        /// <summary>
        /// 获取缓存的视图（用于测试）
        /// </summary>
        /// <param name="subPageId">子页面ID</param>
        /// <returns>缓存的视图，如果不存在则返回null</returns>
        internal UserControl? GetCachedView(string subPageId)
        {
            return _viewCache.TryGetValue(subPageId, out var view) ? view : null;
        }
    }
}
