using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Cascade.Views
{
    public partial class PlayerControl : UserControl
    {
        public PlayerControl()
        {
            InitializeComponent();
        }
    }

    // 简单的转换器，用于根据播放状态切换图标
    public class BoolToGeometryConverter : IValueConverter
    {
        public Geometry TrueGeometry { get; set; }
        public Geometry FalseGeometry { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return TrueGeometry;
            }
            return FalseGeometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
