namespace Cascade.Models
{
    /// <summary>
    /// 通知消息类型
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// 信息
        /// </summary>
        Information,

        /// <summary>
        /// 警告
        /// </summary>
        Warning
    }

    /// <summary>
    /// 通知消息模型
    /// </summary>
    public class NotificationMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// 消息内容（本地化键或直接文本）
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 是否为本地化键
        /// </summary>
        public bool IsLocalizationKey { get; set; }
    }
}
