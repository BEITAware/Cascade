using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cascade.ViewModels.Operations;

namespace Cascade.Models.Operations
{
    /// <summary>
    /// 预设管理器，负责预设的保存、加载、列举和删除。
    /// 预设文件存储在用户数据目录的Presets子文件夹中。
    /// </summary>
    public class PresetManager
    {
        private static PresetManager? _instance;
        private const string PresetFileExtension = ".json";
        
        /// <summary>
        /// 获取PresetManager单例实例
        /// </summary>
        public static PresetManager Instance => _instance ??= new PresetManager();

        /// <summary>
        /// 预设文件存储目录路径
        /// </summary>
        public string PresetsDirectory { get; }

        /// <summary>
        /// 私有构造函数，初始化预设目录路径
        /// </summary>
        private PresetManager()
        {
            // 使用用户本地应用数据目录下的Cascade/Presets文件夹
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            PresetsDirectory = Path.Combine(appDataPath, "Cascade", "Presets");
        }

        /// <summary>
        /// 保存预设到文件
        /// </summary>
        /// <param name="name">预设名称</param>
        /// <param name="context">要保存的操作上下文</param>
        public void SavePreset(string name, OperationContext context)
        {
            try
            {
                // 确保预设目录存在
                if (!Directory.Exists(PresetsDirectory))
                {
                    Directory.CreateDirectory(PresetsDirectory);
                }

                // 序列化上下文数据
                var presetData = context.ExportState();
                var json = JsonSerializer.Serialize(presetData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                // 保存到文件
                var filePath = GetPresetFilePath(name);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存预设失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件加载预设
        /// </summary>
        /// <param name="name">预设名称</param>
        /// <param name="context">要恢复状态的操作上下文</param>
        public void LoadPreset(string name, OperationContext context)
        {
            try
            {
                var filePath = GetPresetFilePath(name);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"预设文件不存在: {name}");
                }

                // 读取并反序列化
                var json = File.ReadAllText(filePath);
                
                // 使用JsonDocument来正确处理嵌套字典
                using var document = JsonDocument.Parse(json);
                var presetData = ConvertJsonElementToDictionary(document.RootElement);
                
                if (presetData != null)
                {
                    context.ImportState(presetData);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载预设失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 将JsonElement转换为Dictionary
        /// </summary>
        private Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object>();
            
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonElementToObject(property.Value);
            }
            
            return result;
        }

        /// <summary>
        /// 将JsonElement转换为对应的.NET对象
        /// </summary>
        private object ConvertJsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict[property.Name] = ConvertJsonElementToObject(property.Value);
                    }
                    return dict;
                    
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElementToObject(item));
                    }
                    return list;
                    
                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;
                    
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    if (element.TryGetDouble(out var doubleValue))
                        return doubleValue;
                    return element.GetDecimal();
                    
                case JsonValueKind.True:
                    return true;
                    
                case JsonValueKind.False:
                    return false;
                    
                case JsonValueKind.Null:
                    return null!;
                    
                default:
                    return element.ToString();
            }
        }

        /// <summary>
        /// 列出所有可用的预设名称
        /// </summary>
        /// <returns>预设名称集合</returns>
        public IEnumerable<string> ListPresets()
        {
            try
            {
                if (!Directory.Exists(PresetsDirectory))
                {
                    return Array.Empty<string>();
                }

                return Directory.GetFiles(PresetsDirectory, $"*{PresetFileExtension}")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToList()!;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 删除指定的预设
        /// </summary>
        /// <param name="name">要删除的预设名称</param>
        public void DeletePreset(string name)
        {
            try
            {
                var filePath = GetPresetFilePath(name);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除预设失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查预设是否存在
        /// </summary>
        /// <param name="name">预设名称</param>
        /// <returns>是否存在</returns>
        public bool PresetExists(string name)
        {
            var filePath = GetPresetFilePath(name);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 获取预设文件的完整路径
        /// </summary>
        /// <param name="name">预设名称</param>
        /// <returns>文件路径</returns>
        private string GetPresetFilePath(string name)
        {
            return Path.Combine(PresetsDirectory, $"{name}{PresetFileExtension}");
        }
    }
}
