using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders.FFmpeg
{
    /// <summary>
    /// 命令片段提供者接口
    /// </summary>
    public interface ICommandSegmentProvider
    {
        /// <summary>
        /// 片段类型
        /// </summary>
        CommandSegmentType SegmentType { get; }
        
        /// <summary>
        /// 优先级（同类型内的排序）
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// 生成命令片段
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>命令片段</returns>
        CommandSegment GenerateSegment(OperationContext context);
        
        /// <summary>
        /// 验证配置
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateConfiguration(OperationContext context);
    }
}