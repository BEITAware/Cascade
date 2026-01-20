using System;
using System.Collections.Generic;
using System.Linq;
using Cascade.Services.CommandBuilders.FFmpeg.Providers;

namespace Cascade.Services.CommandBuilders.FFmpeg
{
    /// <summary>
    /// FFmpeg命令片段提供者注册表
    /// </summary>
    public class FFmpegProviderRegistry
    {
        private static readonly FFmpegProviderRegistry _instance = new FFmpegProviderRegistry();
        public static FFmpegProviderRegistry Instance => _instance;

        private readonly Dictionary<CommandSegmentType, List<ICommandSegmentProvider>> _providers;
        private readonly object _lock = new object();

        private FFmpegProviderRegistry()
        {
            _providers = new Dictionary<CommandSegmentType, List<ICommandSegmentProvider>>();
            RegisterDefaultProviders();
        }

        private void RegisterDefaultProviders()
        {
            Register(new InputFileSegmentProvider());
            Register(new VideoEncoderSegmentProvider());
            Register(new VideoFilterSegmentProvider());
            Register(new OutputOptionsSegmentProvider());
            Register(new OutputFileSegmentProvider());
        }

        /// <summary>
        /// 重置注册表（仅用于测试）
        /// </summary>
        public void ResetForTesting()
        {
            lock (_lock)
            {
                _providers.Clear();
                RegisterDefaultProviders();
            }
        }

        /// <summary>
        /// 注册提供者
        /// </summary>
        /// <param name="provider">提供者实例</param>
        public void Register(ICommandSegmentProvider provider)
        {
            lock (_lock)
            {
                if (!_providers.TryGetValue(provider.SegmentType, out var list))
                {
                    list = new List<ICommandSegmentProvider>();
                    _providers[provider.SegmentType] = list;
                }

                if (!list.Contains(provider))
                {
                    list.Add(provider);
                    // 注册时即排序，确保GetProviders返回有序列表
                    // 优先级高(数值大)的在前? 
                    // 通常Priority属性：数值越大优先级越高。
                    // 之前的代码是 a.Priority.CompareTo(b.Priority) -> 升序 (小到大)
                    // 如果我们想要高优先级先执行/先生效，通常是降序?
                    // 让我们约定：数值越大，优先级越高。
                    // 所以降序排序。
                    list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                }
            }
        }

        /// <summary>
        /// 注销提供者
        /// </summary>
        /// <param name="provider">提供者实例</param>
        public bool Unregister(ICommandSegmentProvider provider)
        {
            lock (_lock)
            {
                if (_providers.TryGetValue(provider.SegmentType, out var list))
                {
                    return list.Remove(provider);
                }
                return false;
            }
        }

        /// <summary>
        /// 获取指定类型的提供者列表
        /// </summary>
        /// <param name="type">片段类型</param>
        /// <returns>提供者列表副本</returns>
        public IReadOnlyList<ICommandSegmentProvider> GetProviders(CommandSegmentType type)
        {
            lock (_lock)
            {
                if (_providers.TryGetValue(type, out var list))
                {
                    return list.ToList(); // 返回副本以保证线程安全
                }
                return new List<ICommandSegmentProvider>();
            }
        }

        /// <summary>
        /// 获取所有类型的提供者
        /// </summary>
        /// <returns>字典副本</returns>
        public Dictionary<CommandSegmentType, List<ICommandSegmentProvider>> GetAllProviders()
        {
            lock (_lock)
            {
                var result = new Dictionary<CommandSegmentType, List<ICommandSegmentProvider>>();
                foreach (var kvp in _providers)
                {
                    result[kvp.Key] = kvp.Value.ToList();
                }
                return result;
            }
        }
    }
}
