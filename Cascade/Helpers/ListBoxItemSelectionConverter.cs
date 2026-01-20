using System;
using System.Globalization;
using System.Windows.Data;

namespace Cascade.Helpers
{
    /// <summary>
    /// 转换器：检查当前项是否应该被选中
    /// 只有当全局选中项与当前项相同时才返回true
    /// </summary>
    public class ListBoxItemSelectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return false;

            var currentItem = values[0]; // 当前ListBoxItem的DataContext
            var selectedItem = values[1]; // 全局选中的项

            // 使用引用相等性比较
            return ReferenceEquals(currentItem, selectedItem);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
