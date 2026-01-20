using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders
{
    /// <summary>
    /// 参数映射器接口
    /// </summary>
    public interface IParameterMapper
    {
        /// <summary>
        /// 映射页面ID
        /// </summary>
        string PageId { get; }
        
        /// <summary>
        /// 映射参数
        /// </summary>
        /// <param name="context">操作上下文</param>
        /// <returns>映射后的参数</returns>
        object MapParameters(OperationContext context);
    }
}