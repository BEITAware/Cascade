using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cascade.Models.Operations
{
    /// <summary>
    /// 操作注册表，管理所有分组和子页面的注册
    /// </summary>
    public class OperationRegistry
    {
        private static OperationRegistry? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static OperationRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new OperationRegistry();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 所有分组集合
        /// </summary>
        public ObservableCollection<OperationGroup> Groups { get; }

        /// <summary>
        /// 私有构造函数，初始化三个默认分组
        /// </summary>
        private OperationRegistry()
        {
            Groups = new ObservableCollection<OperationGroup>
            {
                new OperationGroup
                {
                    Id = "cascade-io",
                    DisplayNameKey = "Operations_Group_CascadeIO",
                    IsExpanded = true
                },
                new OperationGroup
                {
                    Id = "ffmpeg",
                    DisplayNameKey = "Operations_Group_FFmpeg",
                    IsExpanded = true
                },
                new OperationGroup
                {
                    Id = "vapoursynth",
                    DisplayNameKey = "Operations_Group_VapourSynth",
                    IsExpanded = true
                }
            };
        }

        /// <summary>
        /// 获取所有分组
        /// </summary>
        /// <returns>分组集合</returns>
        public ObservableCollection<OperationGroup> GetGroups()
        {
            return Groups;
        }

        /// <summary>
        /// 获取指定分组
        /// </summary>
        /// <param name="groupId">分组ID</param>
        /// <returns>分组对象，如果不存在则返回null</returns>
        public OperationGroup? GetGroup(string groupId)
        {
            return Groups.FirstOrDefault(g => g.Id == groupId);
        }

        /// <summary>
        /// 注册子页面到指定分组
        /// </summary>
        /// <param name="groupId">分组ID</param>
        /// <param name="subPageInfo">子页面信息</param>
        /// <exception cref="ArgumentException">当分组不存在时抛出</exception>
        public void RegisterSubPage(string groupId, OperationSubPageInfo subPageInfo)
        {
            var group = GetGroup(groupId);
            if (group == null)
            {
                throw new ArgumentException($"分组 '{groupId}' 不存在", nameof(groupId));
            }

            // 按Order排序插入
            var insertIndex = group.SubPages.Count;
            for (int i = 0; i < group.SubPages.Count; i++)
            {
                if (group.SubPages[i].Order > subPageInfo.Order)
                {
                    insertIndex = i;
                    break;
                }
            }
            group.SubPages.Insert(insertIndex, subPageInfo);
        }

        /// <summary>
        /// 获取子页面信息
        /// </summary>
        /// <param name="subPageId">子页面ID</param>
        /// <returns>子页面信息，如果不存在则返回null</returns>
        public OperationSubPageInfo? GetSubPageInfo(string subPageId)
        {
            foreach (var group in Groups)
            {
                var subPage = group.SubPages.FirstOrDefault(sp => sp.Id == subPageId);
                if (subPage != null)
                {
                    return subPage;
                }
            }
            return null;
        }

        /// <summary>
        /// 重置注册表（仅用于测试）
        /// </summary>
        internal static void ResetForTesting()
        {
            _instance = null;
        }
    }
}
