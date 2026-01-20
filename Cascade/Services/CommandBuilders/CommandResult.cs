using System.Collections.Generic;

namespace Cascade.Services.CommandBuilders
{
    /// <summary>
    /// 通用命令结果
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// 后端类型
        /// </summary>
        public BackendType Backend { get; set; }
        
        /// <summary>
        /// 命令行或脚本内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 验证结果
        /// </summary>
        public ValidationResult ValidationResult { get; set; } = new();
        
        /// <summary>
        /// 预览文本
        /// </summary>
        public string PreviewText { get; set; } = string.Empty;
        
        /// <summary>
        /// 后端特定的元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}