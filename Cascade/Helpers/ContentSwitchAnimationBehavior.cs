using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cascade.Helpers
{
    /// <summary>
    /// 为ContentControl提供Aero风格的内容切换动画
    /// </summary>
    public static class ContentSwitchAnimationBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ContentSwitchAnimationBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContentControl contentControl)
            {
                if ((bool)e.NewValue)
                {
                    contentControl.Loaded += OnContentControlLoaded;
                }
                else
                {
                    contentControl.Loaded -= OnContentControlLoaded;
                }
            }
        }

        private static void OnContentControlLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ContentControl contentControl)
            {
                // 设置初始变换
                contentControl.RenderTransformOrigin = new Point(0.5, 0.5);
                contentControl.RenderTransform = new ScaleTransform(1, 1);

                // 监听Content变化
                var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    ContentControl.ContentProperty, typeof(ContentControl));
                descriptor?.AddValueChanged(contentControl, OnContentChanged);
            }
        }

        private static void OnContentChanged(object? sender, EventArgs e)
        {
            if (sender is ContentControl contentControl && contentControl.Content != null)
            {
                // 创建Aero风格的进入动画
                var storyboard = new Storyboard();

                // 透明度动画：从0到1
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(350),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(opacityAnimation, contentControl);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(opacityAnimation);

                // 缩放动画X：从0.95到1
                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(350),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleXAnimation, contentControl);
                Storyboard.SetTargetProperty(scaleXAnimation, 
                    new PropertyPath("RenderTransform.ScaleX"));
                storyboard.Children.Add(scaleXAnimation);

                // 缩放动画Y：从0.95到1
                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(350),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleYAnimation, contentControl);
                Storyboard.SetTargetProperty(scaleYAnimation, 
                    new PropertyPath("RenderTransform.ScaleY"));
                storyboard.Children.Add(scaleYAnimation);

                // 启动动画
                storyboard.Begin();
            }
        }
    }
}
