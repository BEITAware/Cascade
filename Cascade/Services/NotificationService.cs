using System;
using Cascade.Models;

namespace Cascade.Services
{
    /// <summary>
    /// 通知服务，用于在应用程序中发送信息和警告消息
    /// </summary>
    public static class NotificationService
    {
        /// <summary>
        /// 通知消息变更事件
        /// </summary>
        public static event EventHandler<NotificationMessage>? NotificationChanged;

        /// <summary>
        /// 发送信息消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="isLocalizationKey">是否为本地化键（默认为 true）</param>
        public static void SendInformation(string message, bool isLocalizationKey = true)
        {
            var notification = new NotificationMessage
            {
                Type = NotificationType.Information,
                Message = message,
                IsLocalizationKey = isLocalizationKey
            };

            NotificationChanged?.Invoke(null, notification);
        }

        /// <summary>
        /// 发送警告消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="isLocalizationKey">是否为本地化键（默认为 true）</param>
        public static void SendWarning(string message, bool isLocalizationKey = true)
        {
            var notification = new NotificationMessage
            {
                Type = NotificationType.Warning,
                Message = message,
                IsLocalizationKey = isLocalizationKey
            };

            NotificationChanged?.Invoke(null, notification);
        }

        /// <summary>
        /// 清除当前通知，恢复默认状态消息
        /// </summary>
        public static void ClearNotification()
        {
            SendInformation("Status_Ready");
        }
    }
}
