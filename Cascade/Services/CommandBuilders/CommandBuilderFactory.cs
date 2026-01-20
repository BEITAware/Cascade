using System;
using System.Collections.Generic;
using System.Linq;

namespace Cascade.Services.CommandBuilders
{
    /// <summary>
    /// 命令构建器工厂
    /// </summary>
    public class CommandBuilderFactory
    {
        private readonly Dictionary<BackendType, Func<ICommandProvider>> _builders;
        
        public CommandBuilderFactory()
        {
            _builders = new Dictionary<BackendType, Func<ICommandProvider>>();
            RegisterDefaultBuilders();
        }
        
        /// <summary>
        /// 注册默认构建器
        /// </summary>
        private void RegisterDefaultBuilders()
        {
            RegisterBuilder(BackendType.FFmpeg, () => new FFmpeg.FFmpegCommandBuilder());
            // 未来扩展: RegisterBuilder(BackendType.VapourSynth, () => new VapourSynth.VapourSynthScriptBuilder());
        }
        
        /// <summary>
        /// 注册新的构建器
        /// </summary>
        /// <param name="backend">后端类型</param>
        /// <param name="factory">构建器工厂方法</param>
        public void RegisterBuilder(BackendType backend, Func<ICommandProvider> factory)
        {
            _builders[backend] = factory;
        }
        
        /// <summary>
        /// 注销构建器
        /// </summary>
        /// <param name="backend">后端类型</param>
        /// <returns>是否成功注销</returns>
        public bool UnregisterBuilder(BackendType backend)
        {
            return _builders.Remove(backend);
        }
        
        /// <summary>
        /// 创建构建器实例
        /// </summary>
        /// <param name="backend">后端类型</param>
        /// <returns>命令提供者实例</returns>
        /// <exception cref="NotSupportedException">当后端不支持时抛出</exception>
        public ICommandProvider CreateBuilder(BackendType backend)
        {
            if (_builders.TryGetValue(backend, out var factory))
            {
                return factory();
            }
            throw new NotSupportedException($"Backend {backend} is not supported");
        }
        
        /// <summary>
        /// 获取所有支持的后端
        /// </summary>
        /// <returns>支持的后端类型列表</returns>
        public IReadOnlyList<BackendType> GetSupportedBackends()
        {
            return _builders.Keys.ToList();
        }
        
        /// <summary>
        /// 检查是否支持指定后端
        /// </summary>
        /// <param name="backend">后端类型</param>
        /// <returns>是否支持</returns>
        public bool IsBackendSupported(BackendType backend)
        {
            return _builders.ContainsKey(backend);
        }
    }
}