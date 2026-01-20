using System;
using System.Collections.Generic;
using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders
{
    /// <summary>
    /// 抽象命令构建器基类
    /// </summary>
    public abstract class CommandBuilderBase : ICommandProvider
    {
        protected readonly Dictionary<string, IParameterMapper> ParameterMappers;
        
        public abstract BackendType SupportedBackend { get; }
        public abstract string DisplayName { get; }
        
        protected CommandBuilderBase()
        {
            ParameterMappers = new Dictionary<string, IParameterMapper>();
            RegisterParameterMappers();
        }
        
        /// <summary>
        /// 注册参数映射器
        /// </summary>
        protected abstract void RegisterParameterMappers();
        
        public abstract CommandResult GenerateCommand(OperationContext context);
        public abstract ValidationResult ValidateConfiguration(OperationContext context);
        public abstract string GetPreview(OperationContext context);
        public abstract IReadOnlyList<string> GetSupportedPages();
        
        /// <summary>
        /// 获取本地化服务实例
        /// </summary>
        protected static string GetLocalizedString(string key) => Services.LocalizationService.GetString(key);
    }
}