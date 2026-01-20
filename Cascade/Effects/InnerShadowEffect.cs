using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Cascade.Effects
{
    /// <summary>
    /// 内阴影装饰器 - 在元素内部边缘创建阴影效果
    /// 使用方法：将此控件包裹在需要内阴影的元素外层
    /// </summary>
    public class InnerShadowDecorator : Decorator
    {
        private readonly Grid _container;
        private readonly Rectangle _shadowRect;
        private readonly Border _clipBorder;

        public InnerShadowDecorator()
        {
            _container = new Grid();
            _shadowRect = new Rectangle();
            _clipBorder = new Border();

            // 设置阴影矩形
            _shadowRect.Fill = Brushes.Transparent;
            _shadowRect.StrokeThickness = 20;
            _shadowRect.Stroke = Brushes.Black;
            _shadowRect.Effect = new BlurEffect { Radius = 10 };
            _shadowRect.IsHitTestVisible = false;
            _shadowRect.Margin = new Thickness(-10);

            // 裁剪边框
            _clipBorder.ClipToBounds = true;
            _clipBorder.Child = _shadowRect;

            UpdateShadow();
        }

        #region Dependency Properties

        public static readonly DependencyProperty ShadowColorProperty =
            DependencyProperty.Register(nameof(ShadowColor), typeof(Color), typeof(InnerShadowDecorator),
                new PropertyMetadata(Colors.Black, OnShadowPropertyChanged));

        public Color ShadowColor
        {
            get => (Color)GetValue(ShadowColorProperty);
            set => SetValue(ShadowColorProperty, value);
        }

        public static readonly DependencyProperty BlurRadiusProperty =
            DependencyProperty.Register(nameof(BlurRadius), typeof(double), typeof(InnerShadowDecorator),
                new PropertyMetadata(10.0, OnShadowPropertyChanged));

        public double BlurRadius
        {
            get => (double)GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        public static readonly DependencyProperty ShadowDepthProperty =
            DependencyProperty.Register(nameof(ShadowDepth), typeof(double), typeof(InnerShadowDecorator),
                new PropertyMetadata(5.0, OnShadowPropertyChanged));

        public double ShadowDepth
        {
            get => (double)GetValue(ShadowDepthProperty);
            set => SetValue(ShadowDepthProperty, value);
        }

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(nameof(Direction), typeof(double), typeof(InnerShadowDecorator),
                new PropertyMetadata(315.0, OnShadowPropertyChanged));

        /// <summary>
        /// 阴影方向（角度，0-360，315为右下方光源）
        /// </summary>
        public double Direction
        {
            get => (double)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(InnerShadowDecorator),
                new PropertyMetadata(0.6, OnShadowPropertyChanged));

        public double ShadowOpacity
        {
            get => (double)GetValue(ShadowOpacityProperty);
            set => SetValue(ShadowOpacityProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(InnerShadowDecorator),
                new PropertyMetadata(new CornerRadius(0), OnShadowPropertyChanged));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        private static void OnShadowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((InnerShadowDecorator)d).UpdateShadow();
        }

        private void UpdateShadow()
        {
            var color = ShadowColor;
            color.A = (byte)(ShadowOpacity * 255);
            _shadowRect.Stroke = new SolidColorBrush(color);
            _shadowRect.StrokeThickness = ShadowDepth + BlurRadius;

            if (_shadowRect.Effect is BlurEffect blur)
            {
                blur.Radius = BlurRadius;
            }

            // 计算偏移
            var radians = Direction * System.Math.PI / 180;
            var offsetX = -System.Math.Cos(radians) * ShadowDepth / 2;
            var offsetY = System.Math.Sin(radians) * ShadowDepth / 2;
            _shadowRect.Margin = new Thickness(-BlurRadius + offsetX, -BlurRadius + offsetY, 
                                                -BlurRadius - offsetX, -BlurRadius - offsetY);

            // 设置圆角
            _shadowRect.RadiusX = CornerRadius.TopLeft;
            _shadowRect.RadiusY = CornerRadius.TopLeft;
            _clipBorder.CornerRadius = CornerRadius;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Child != null)
            {
                Child.Measure(constraint);
                return Child.DesiredSize;
            }
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (Child != null)
            {
                Child.Arrange(new Rect(arrangeSize));
            }
            _clipBorder.Arrange(new Rect(arrangeSize));
            return arrangeSize;
        }

        protected override int VisualChildrenCount => Child != null ? 2 : 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && Child != null) return Child;
            if (index == 0 || index == 1) return _clipBorder;
            throw new System.ArgumentOutOfRangeException(nameof(index));
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            if (visualAdded != _clipBorder)
            {
                AddVisualChild(_clipBorder);
            }
        }
    }
}
