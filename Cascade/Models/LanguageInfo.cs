namespace Cascade.Models
{
    /// <summary>
    /// 语言信息模型，表示一种支持的语言
    /// </summary>
    public class LanguageInfo
    {
        /// <summary>
        /// 区域性代码，如 "zh-CN" 或 "en-US"
        /// </summary>
        public string CultureCode { get; set; } = string.Empty;

        /// <summary>
        /// 语言的本地名称，如 "简体中文"
        /// </summary>
        public string NativeName { get; set; } = string.Empty;

        /// <summary>
        /// 语言的英文名称，如 "Chinese (Simplified)"
        /// </summary>
        public string EnglishName { get; set; } = string.Empty;
    }
}
