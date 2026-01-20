using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cascade.Helpers
{
    /// <summary>
    /// ListBox 附加行为集合
    /// </summary>
    public static class ListBoxBehaviors
    {
        #region SelectedItems 附加属性

        /// <summary>
        /// 绑定 ListBox 的多选项到 ViewModel
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(ObservableCollection<object>),
                typeof(ListBoxBehaviors),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static ObservableCollection<object> GetSelectedItems(DependencyObject obj)
            => (ObservableCollection<object>)obj.GetValue(SelectedItemsProperty);

        public static void SetSelectedItems(DependencyObject obj, ObservableCollection<object> value)
            => obj.SetValue(SelectedItemsProperty, value);

        // 用于标记是否正在同步，避免循环更新
        private static readonly DependencyProperty IsSyncingProperty =
            DependencyProperty.RegisterAttached(
                "IsSyncing",
                typeof(bool),
                typeof(ListBoxBehaviors),
                new PropertyMetadata(false));

        private static bool GetIsSyncing(DependencyObject obj)
            => (bool)obj.GetValue(IsSyncingProperty);

        private static void SetIsSyncing(DependencyObject obj, bool value)
            => obj.SetValue(IsSyncingProperty, value);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox) return;

            // 移除旧的事件处理
            listBox.SelectionChanged -= ListBox_SelectionChanged;
            
            if (e.OldValue is ObservableCollection<object> oldCollection)
            {
                oldCollection.CollectionChanged -= OnViewModelCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<object> newCollection)
            {
                // 添加新的事件处理
                listBox.SelectionChanged += ListBox_SelectionChanged;
                newCollection.CollectionChanged += OnViewModelCollectionChanged;

                // 初始化选中项
                SyncViewModelToView(listBox, newCollection);
            }
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            if (GetIsSyncing(listBox)) return;

            var collection = GetSelectedItems(listBox);
            if (collection == null) return;

            // 使用 Dispatcher 延迟更新，避免在 CollectionChanged 事件中修改集合
            listBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncViewToViewModel(listBox, collection);
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private static void OnViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is not ObservableCollection<object> collection) return;

            // 找到关联的 ListBox
            // 注意：这里需要遍历所有可能的 ListBox，但为了性能，我们使用 Dispatcher
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 这个方法会在 ViewModel 集合变化时被调用
                // 但我们不需要在这里做任何事，因为同步是单向的（View -> ViewModel）
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private static void SyncViewToViewModel(ListBox listBox, ObservableCollection<object> collection)
        {
            if (GetIsSyncing(listBox)) return;

            try
            {
                SetIsSyncing(listBox, true);

                // 同步选中项到 ViewModel
                collection.Clear();
                foreach (var item in listBox.SelectedItems)
                {
                    collection.Add(item);
                }
            }
            finally
            {
                SetIsSyncing(listBox, false);
            }
        }

        private static void SyncViewModelToView(ListBox listBox, ObservableCollection<object> collection)
        {
            if (GetIsSyncing(listBox)) return;

            try
            {
                SetIsSyncing(listBox, true);

                // 同步 ViewModel 到选中项
                listBox.SelectedItems.Clear();
                foreach (var item in collection)
                {
                    if (listBox.Items.Contains(item))
                    {
                        listBox.SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                SetIsSyncing(listBox, false);
            }
        }

        #endregion

        #region ClearSelectionIfNotInItems 附加属性

        /// <summary>
        /// 启用"清除不在列表中的选择"行为：
        /// 当 SelectedItem 不在 ItemsSource 中时，自动清除选择。
        /// 这可以防止多个 ListBox 绑定到同一个 SelectedItem 时出现多选问题。
        /// </summary>
        public static readonly DependencyProperty ClearSelectionIfNotInItemsProperty =
            DependencyProperty.RegisterAttached(
                "ClearSelectionIfNotInItems",
                typeof(bool),
                typeof(ListBoxBehaviors),
                new PropertyMetadata(false, OnClearSelectionIfNotInItemsChanged));

        public static bool GetClearSelectionIfNotInItems(DependencyObject obj)
            => (bool)obj.GetValue(ClearSelectionIfNotInItemsProperty);

        public static void SetClearSelectionIfNotInItems(DependencyObject obj, bool value)
            => obj.SetValue(ClearSelectionIfNotInItemsProperty, value);

        private static void OnClearSelectionIfNotInItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox) return;

            if ((bool)e.NewValue)
            {
                // 监听 SelectedItem 属性变化
                var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    ListBox.SelectedItemProperty, typeof(ListBox));
                descriptor?.AddValueChanged(listBox, ListBox_SelectedItemChanged);
            }
            else
            {
                var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    ListBox.SelectedItemProperty, typeof(ListBox));
                descriptor?.RemoveValueChanged(listBox, ListBox_SelectedItemChanged);
            }
        }

        private static void ListBox_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (sender is not ListBox listBox) return;

            // 如果有选中项，检查它是否在 ItemsSource 中
            if (listBox.SelectedItem != null)
            {
                var itemsSource = listBox.ItemsSource;
                if (itemsSource != null)
                {
                    bool found = false;
                    foreach (var item in itemsSource)
                    {
                        if (ReferenceEquals(item, listBox.SelectedItem))
                        {
                            found = true;
                            break;
                        }
                    }

                    // 如果选中项不在 ItemsSource 中，清除选择（不触发绑定更新）
                    if (!found)
                    {
                        // 暂时移除绑定
                        var binding = BindingOperations.GetBindingExpression(listBox, ListBox.SelectedItemProperty);
                        if (binding != null)
                        {
                            var bindingBase = binding.ParentBinding;
                            BindingOperations.ClearBinding(listBox, ListBox.SelectedItemProperty);
                            listBox.SelectedItem = null;
                            // 恢复绑定
                            BindingOperations.SetBinding(listBox, ListBox.SelectedItemProperty, bindingBase);
                        }
                        else
                        {
                            listBox.SelectedItem = null;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
