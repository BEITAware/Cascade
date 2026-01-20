using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cascade.Helpers
{
    /// <summary>
    /// TextBox 附加行为集合
    /// </summary>
    public static class TextBoxBehaviors
    {
        #region SelectAllOnFirstClick 附加属性

        /// <summary>
        /// 启用"首次点击全选"行为：
        /// - 第一次点击：全选文本
        /// - 第二次点击：定位光标到点击位置
        /// 
        /// 适用于数值输入框等场景，用户通常想要替换整个值而非编辑部分内容。
        /// </summary>
        public static readonly DependencyProperty SelectAllOnFirstClickProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFirstClick",
                typeof(bool),
                typeof(TextBoxBehaviors),
                new PropertyMetadata(false, OnSelectAllOnFirstClickChanged));

        public static bool GetSelectAllOnFirstClick(DependencyObject obj)
            => (bool)obj.GetValue(SelectAllOnFirstClickProperty);

        public static void SetSelectAllOnFirstClick(DependencyObject obj, bool value)
            => obj.SetValue(SelectAllOnFirstClickProperty, value);

        private static void OnSelectAllOnFirstClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            if ((bool)e.NewValue)
            {
                textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
                textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
            }
            else
            {
                textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
                textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
            }
        }

        private static void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            // 如果 TextBox 还没有键盘焦点，则全选并获取焦点
            if (!textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true; // 阻止默认的光标定位行为
            }
            // 如果已有焦点，允许正常的光标定位（第二次点击）
        }

        private static void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        #endregion
    }
}
