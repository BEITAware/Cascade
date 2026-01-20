using System;
using System.Collections.Generic;

namespace Cascade.Services.CommandBuilders
{
    /// <summary>
    /// 后端类型枚举
    /// </summary>
    public enum BackendType
    {
        FFmpeg,
        VapourSynth,
        // 未来可扩展: AviSynth, x264, x265, etc.
    }

    /// <summary>
    /// 统一的命令提供者接口
    /// </summary>
    public interface ICommandProvider
    {
        /// <summary>
        /// 支持的后端类型
        /// </summary>
        BackendType SupportedBackend { get; }
        
        /// <summary>
        /// 后端显示名称（本地化）
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// 生成命令或脚本
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>命令结果</returns>
        CommandResult GenerateCommand(ViewModels.Operations.OperationContext context);
        
        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateConfiguration(ViewModels.Operations.OperationContext context);
        
        /// <summary>
        /// 获取预览文本
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>预览文本</returns>
        string GetPreview(ViewModels.Operations.OperationContext context);
        
        /// <summary>
        /// 获取支持的操作页面列表
        /// </summary>
        /// <returns>页面ID列表</returns>
        IReadOnlyList<string> GetSupportedPages();
    }
}