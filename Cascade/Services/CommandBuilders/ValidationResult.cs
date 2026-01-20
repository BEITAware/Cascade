using System.Collections.Generic;
using System.Linq;

namespace Cascade.Services.CommandBuilders
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; } = true;
        
        /// <summary>
        /// 错误消息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// 警告消息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// 添加错误消息
        /// </summary>
        /// <param name="error">错误消息</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }
        
        /// <summary>
        /// 添加警告消息
        /// </summary>
        /// <param name="warning">警告消息</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// 是否有任何消息（错误或警告）
        /// </summary>
        public bool HasMessages => Errors.Any() || Warnings.Any();
        
        /// <summary>
        /// 合并另一个验证结果
        /// </summary>
        /// <param name="other">要合并的验证结果</param>
        public void Merge(ValidationResult other)
        {
            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
            if (!other.IsValid)
            {
                IsValid = false;
            }
        }
    }
}