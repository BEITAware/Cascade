using System;
using Cascade.Services;

namespace Cascade.Models.Operations
{
    /// <summary>
    /// 操作子页面信息模型，描述一个子页面的元数据
    /// </summary>
    public class OperationSubPageInfo
    {
        /// <summary>
        /// 子页面唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 本地化资源键，用于获取本地化的显示名称
        /// </summary>
        public string DisplayNameKey { get; set; } = string.Empty;

        /// <summary>
        /// 子页面显示名称（从本地化资源获取）
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(DisplayNameKey) 
            ? Id 
            : LocalizationService.GetString(DisplayNameKey);

        /// <summary>
        /// 子页面View类型
        /// </summary>
        public Type? ViewType { get; set; }

        /// <summary>
        /// 子页面ViewModel类型
        /// </summary>
        public Type? ViewModelType { get; set; }

        /// <summary>
        /// 排序顺序，数值越小越靠前
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 可见性条件，根据OperationContext判断是否显示该子页面
        /// 返回true表示显示，返回false表示隐藏
        /// 如果为null，则始终显示
        /// </summary>
        public Func<object, bool>? VisibilityCondition { get; set; }
    }
}
