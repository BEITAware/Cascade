using System;
using System.Runtime.InteropServices;

namespace Cascade.Helpers
{
    /// <summary>
    /// 窗口模糊效果辅助类
    /// </summary>
    public static class WindowBlurHelper
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        /// <summary>
        /// 为指定窗口句柄启用模糊效果
        /// </summary>
        public static void EnableBlur(IntPtr hwnd)
        {
            EnableBlur(hwnd, 0x80000000); // 默认半透明黑色
        }

        /// <summary>
        /// 为指定窗口句柄启用带颜色的模糊效果
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="gradientColor">ABGR 格式的颜色值</param>
        public static void EnableBlur(IntPtr hwnd, uint gradientColor)
        {
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = (int)gradientColor;
            accent.AccentFlags = 2; // 启用模糊
            
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(hwnd, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }
    }
}
