using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace Cascade.Helpers
{
    public static class PopupBlurHelper
    {
        public static readonly DependencyProperty IsBlurEnabledProperty =
            DependencyProperty.RegisterAttached("IsBlurEnabled", typeof(bool), typeof(PopupBlurHelper), new PropertyMetadata(false, OnIsBlurEnabledChanged));

        public static bool GetIsBlurEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsBlurEnabledProperty);
        }

        public static void SetIsBlurEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsBlurEnabledProperty, value);
        }

        private static void OnIsBlurEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Popup popup)
            {
                if ((bool)e.NewValue)
                {
                    popup.Opened += Popup_Opened;
                }
                else
                {
                    popup.Opened -= Popup_Opened;
                }
            }
        }

        private static void Popup_Opened(object? sender, EventArgs e)
        {
            if (sender is Popup popup && popup.Child != null)
            {
                var source = PresentationSource.FromVisual(popup.Child) as HwndSource;
                if (source != null)
                {
                    // 使用 ABGR 格式: 0xAA_BB_GG_RR
                    // 深蓝色半透明: Alpha=0xC0, Blue=0x40, Green=0x30, Red=0x20
                    WindowBlurHelper.EnableBlur(source.Handle, 0xC0403020);
                }
            }
        }
    }
}
