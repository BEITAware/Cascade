using System.Windows;

namespace Cascade.Helpers
{
    /// <summary>
    /// 选择辅助类，用于在ItemsControl中标记选中项
    /// </summary>
    public static class SelectionHelper
    {
        /// <summary>
        /// 标识当前项是否被选中的附加属性
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.RegisterAttached(
                "IsSelected",
                typeof(bool),
                typeof(SelectionHelper),
                new PropertyMetadata(false));

        public static bool GetIsSelected(DependencyObject obj)
            => (bool)obj.GetValue(IsSelectedProperty);

        public static void SetIsSelected(DependencyObject obj, bool value)
            => obj.SetValue(IsSelectedProperty, value);
    }
}
