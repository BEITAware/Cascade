using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Cascade.ViewModels.Operations
{
    /// <summary>
    /// 子页面ViewModel基类，所有操作子页面的ViewModel必须继承此类。
    /// 提供对OperationContext的访问、序列化/反序列化支持以及属性变更通知。
    /// </summary>
    public abstract class OperationViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed;
        private bool _isLoadingFromContext;

        /// <summary>
        /// 获取操作上下文实例，用于子页面间通信
        /// </summary>
        public OperationContext Context => OperationContext.Instance;

        /// <summary>
        /// 子页面唯一标识符
        /// </summary>
        public abstract string SubPageId { get; }

        /// <summary>
        /// 序列化版本号，用于预设兼容性处理。默认值为1。
        /// </summary>
        public virtual int Version => 1;

        /// <summary>
        /// 构造函数 - 订阅SharedDataChanged事件以支持预设加载
        /// </summary>
        protected OperationViewModelBase()
        {
            Context.SharedDataChanged += OnContextDataChanged;
            
            // 初始化时从Context加载数据（如果有的话）
            // 这确保了即使ViewModel在预设加载后才创建，也能获取到正确的数据
            LoadFromContext();
        }

        /// <summary>
        /// 当Context中的共享数据变更时调用
        /// </summary>
        private void OnContextDataChanged(object? sender, SharedDataChangedEventArgs e)
        {
            // 只处理与当前页面相关的数据变更
            if (e.PageId == SubPageId && e.Key == null)
            {
                // Key为null表示整个页面的数据被替换（预设加载）
                LoadFromContext();
            }
        }

        /// <summary>
        /// 从Context加载数据到ViewModel
        /// 子类应该重写此方法以实现自定义的加载逻辑
        /// </summary>
        protected virtual void LoadFromContext()
        {
            // 默认实现：使用反射从Context读取所有已发布的数据
            _isLoadingFromContext = true;
            try
            {
                var pageData = Context.GetPageData(SubPageId);
                
                // 如果没有数据，直接返回
                if (pageData.Count == 0)
                {
                    return;
                }
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{SubPageId}] LoadFromContext: Loading {pageData.Count} properties");
#endif
                
                foreach (var kvp in pageData)
                {
                    var property = GetType().GetProperty(kvp.Key);
                    if (property != null && property.CanWrite)
                    {
                        try
                        {
                            var targetType = property.PropertyType;
                            var value = kvp.Value;
                            
                            // 处理类型转换
                            if (value != null && !targetType.IsAssignableFrom(value.GetType()))
                            {
                                // 尝试Convert.ChangeType
                                if (targetType.IsEnum)
                                {
                                    value = Enum.ToObject(targetType, Convert.ToInt32(value));
                                }
                                else
                                {
                                    value = Convert.ChangeType(value, targetType);
                                }
                            }
                            
                            property.SetValue(this, value);
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[{SubPageId}] LoadFromContext: Set {kvp.Key} = {value}");
#endif
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[{SubPageId}] LoadFromContext: Failed to set {kvp.Key}: {ex.Message}");
#endif
                            // 类型转换失败，跳过
                        }
                    }
                }
            }
            finally
            {
                _isLoadingFromContext = false;
            }
        }

        /// <summary>
        /// 将当前ViewModel状态序列化为JSON对象
        /// </summary>
        /// <returns>包含ViewModel状态的JsonObject</returns>
        public abstract JsonObject Serialize();

        /// <summary>
        /// 从JSON对象反序列化恢复ViewModel状态
        /// </summary>
        /// <param name="data">包含ViewModel状态的JsonObject</param>
        public abstract void Deserialize(JsonObject data);

        #region 共享数据便捷方法

        /// <summary>
        /// 发布当前页面的共享数据
        /// </summary>
        /// <param name="key">数据键</param>
        /// <param name="value">数据值</param>
        protected void PublishData(string key, object value)
        {
            Context.PublishData(SubPageId, key, value);
        }

        /// <summary>
        /// 批量发布当前页面的共享数据
        /// </summary>
        /// <param name="data">数据字典</param>
        protected void PublishData(Dictionary<string, object> data)
        {
            Context.PublishData(SubPageId, data);
        }

        /// <summary>
        /// 移除当前页面的某个共享数据
        /// </summary>
        /// <param name="key">数据键</param>
        /// <returns>是否成功移除</returns>
        protected bool RemoveData(string key)
        {
            return Context.RemoveData(SubPageId, key);
        }

        /// <summary>
        /// 清除当前页面的所有共享数据
        /// </summary>
        protected void ClearMyData()
        {
            Context.ClearPageData(SubPageId);
        }

        /// <summary>
        /// 获取其他页面的共享数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="pageId">页面ID</param>
        /// <param name="key">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值</returns>
        protected T? GetDataFrom<T>(string pageId, string key, T? defaultValue = default)
        {
            return Context.GetData<T>(pageId, key, defaultValue);
        }

        /// <summary>
        /// 跨页面搜索数据
        /// </summary>
        /// <param name="key">数据键</param>
        /// <returns>包含该键的页面ID和对应值的字典</returns>
        protected IReadOnlyDictionary<string, object> FindDataByKey(string key)
        {
            return Context.FindDataByKey(key);
        }

        /// <summary>
        /// 订阅共享数据变更事件
        /// </summary>
        /// <param name="handler">事件处理器</param>
        protected void SubscribeToDataChanges(EventHandler<SharedDataChangedEventArgs> handler)
        {
            Context.SharedDataChanged += handler;
        }

        /// <summary>
        /// 取消订阅共享数据变更事件
        /// </summary>
        /// <param name="handler">事件处理器</param>
        protected void UnsubscribeFromDataChanges(EventHandler<SharedDataChangedEventArgs> handler)
        {
            Context.SharedDataChanged -= handler;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (object.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            
            // 自动发布到OperationContext（排除某些属性，且不在从Context加载时发布）
            if (!_isLoadingFromContext && propertyName != null && ShouldPublishProperty(propertyName))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{SubPageId}] Auto-publishing: {propertyName} = {value}");
#endif
                PublishData(propertyName, value!);
            }
            
            return true;
        }

        /// <summary>
        /// 判断属性是否应该发布到OperationContext
        /// 子类可以重写此方法来自定义发布行为
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <returns>是否应该发布</returns>
        protected virtual bool ShouldPublishProperty(string propertyName)
        {
            // 排除UI相关的属性和基类属性
            var excludedProperties = new[]
            {
                "Context",
                "SubPageId",
                "Version"
            };
            
            return !excludedProperties.Contains(propertyName);
        }

        /// <summary>
        /// 释放资源，清理共享数据
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的虚方法，子类可重写
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消订阅事件
                    Context.SharedDataChanged -= OnContextDataChanged;
                    
                    // 清理当前页面的共享数据
                    ClearMyData();
                }
                _disposed = true;
            }
        }
    }
}
