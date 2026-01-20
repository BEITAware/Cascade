using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Cascade.Models;

namespace Cascade.Services
{
    /// <summary>
    /// 本地化服务，负责管理语言资源加载和切换
    /// </summary>
    public static class LocalizationService
    {
        private const string DefaultLanguage = "zh-CN";
        private const string SettingsFileName = "settings.json";
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cascade");
        
        private static string _currentLanguage = DefaultLanguage;
        private static ResourceDictionary? _currentLanguageDictionary;

        /// <summary>
        /// 当前语言代码
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// 支持的语言列表
        /// </summary>
        public static IReadOnlyList<LanguageInfo> SupportedLanguages { get; } = new List<LanguageInfo>
        {
            new LanguageInfo { CultureCode = "zh-CN", NativeName = "简体中文", EnglishName = "Chinese (Simplified)" },
            new LanguageInfo { CultureCode = "en-US", NativeName = "English", EnglishName = "English (US)" }
        };

        /// <summary>
        /// 语言变更事件
        /// </summary>
        public static event EventHandler<string>? LanguageChanged;

        /// <summary>
        /// 初始化本地化服务
        /// </summary>
        public static void Initialize()
        {
            // 查找并记录 App.xaml 中预加载的语言资源字典
            if (Application.Current != null)
            {
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    if (dict.Source != null && dict.Source.OriginalString.Contains("/Resources/Strings/Strings."))
                    {
                        _currentLanguageDictionary = dict;
                        break;
                    }
                }
            }
            
            var savedLanguage = LoadLanguageSetting();
            SetLanguage(savedLanguage);
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="cultureCode">语言代码，如 "zh-CN" 或 "en-US"</param>
        public static void SetLanguage(string cultureCode)
        {
            // 验证语言代码是否支持，不支持则回退到默认语言
            if (!IsLanguageSupported(cultureCode))
            {
                cultureCode = DefaultLanguage;
            }

            // 如果没有 Application.Current (例如在测试环境中)，仅更新状态并触发事件
            if (Application.Current == null)
            {
                _currentLanguage = cultureCode;
                LanguageChanged?.Invoke(null, cultureCode);
                return;
            }

            // 尝试加载语言资源
            var resourceUri = GetLanguageResourceUri(cultureCode);
            ResourceDictionary? newDictionary = null;

            try
            {
                newDictionary = new ResourceDictionary { Source = resourceUri };
            }
            catch
            {
                // 如果加载失败，尝试回退到默认语言
                if (cultureCode != DefaultLanguage)
                {
                    try
                    {
                        resourceUri = GetLanguageResourceUri(DefaultLanguage);
                        newDictionary = new ResourceDictionary { Source = resourceUri };
                        cultureCode = DefaultLanguage;
                    }
                    catch
                    {
                        // 默认语言也加载失败，不做任何操作
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // 更新应用程序资源
            if (newDictionary != null)
            {
                // 移除旧的语言资源字典
                if (_currentLanguageDictionary != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_currentLanguageDictionary);
                }

                // 添加新的语言资源字典
                Application.Current.Resources.MergedDictionaries.Add(newDictionary);
                _currentLanguageDictionary = newDictionary;
            }

            _currentLanguage = cultureCode;
            LanguageChanged?.Invoke(null, cultureCode);
        }


        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        /// <param name="key">资源键</param>
        /// <returns>本地化字符串，如果键不存在则返回键名本身</returns>
        public static string GetString(string key)
        {
            if (Application.Current == null)
            {
                return key;
            }

            var resource = Application.Current.TryFindResource(key);
            return resource as string ?? key;
        }

        /// <summary>
        /// 保存语言设置到配置文件
        /// </summary>
        public static void SaveLanguageSetting()
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                var settingsPath = Path.Combine(SettingsDirectory, SettingsFileName);
                var settings = new LanguageSettings { Language = _currentLanguage };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // 保存失败时静默处理
            }
        }

        /// <summary>
        /// 从配置文件加载语言设置
        /// </summary>
        /// <returns>保存的语言代码，如果加载失败则返回默认语言</returns>
        public static string LoadLanguageSetting()
        {
            try
            {
                var settingsPath = Path.Combine(SettingsDirectory, SettingsFileName);
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<LanguageSettings>(json);
                    if (settings != null && !string.IsNullOrEmpty(settings.Language))
                    {
                        return settings.Language;
                    }
                }
            }
            catch
            {
                // 加载失败时返回默认语言
            }

            return DefaultLanguage;
        }

        /// <summary>
        /// 检查语言是否支持
        /// </summary>
        private static bool IsLanguageSupported(string cultureCode)
        {
            foreach (var lang in SupportedLanguages)
            {
                if (lang.CultureCode == cultureCode)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取语言资源文件的 URI
        /// </summary>
        private static Uri GetLanguageResourceUri(string cultureCode)
        {
            return new Uri($"pack://application:,,,/Resources/Strings/Strings.{cultureCode}.xaml", UriKind.Absolute);
        }

        /// <summary>
        /// 语言设置数据类
        /// </summary>
        private class LanguageSettings
        {
            public string Language { get; set; } = DefaultLanguage;
        }
    }
}
