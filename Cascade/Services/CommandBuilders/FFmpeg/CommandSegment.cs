namespace Cascade.Services.CommandBuilders.FFmpeg
{
    /// <summary>
    /// 命令片段类型枚举
    /// </summary>
    public enum CommandSegmentType
    {
        /// <summary>
        /// 全局选项（如 -y, -hide_banner）
        /// </summary>
        GlobalOptions = 0,
        
        /// <summary>
        /// 输入文件和选项
        /// </summary>
        Input = 1,
        
        /// <summary>
        /// 视频滤镜
        /// </summary>
        VideoFilters = 2,
        
        /// <summary>
        /// 视频编码器
        /// </summary>
        VideoCodec = 3,
        
        /// <summary>
        /// 音频编码器
        /// </summary>
        AudioCodec = 4,
        
        /// <summary>
        /// 输出选项
        /// </summary>
        OutputOptions = 5,
        
        /// <summary>
        /// 输出文件
        /// </summary>
        OutputFile = 6
    }

    /// <summary>
    /// FFmpeg命令片段
    /// </summary>
    public class CommandSegment
    {
        /// <summary>
        /// 片段类型
        /// </summary>
        public CommandSegmentType Type { get; set; }
        
        /// <summary>
        /// 参数字符串
        /// </summary>
        public string Parameters { get; set; } = string.Empty;
        
        /// <summary>
        /// 优先级（同类型内的排序）
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; } = true;
        
        /// <summary>
        /// 验证消息
        /// </summary>
        public string ValidationMessage { get; set; } = string.Empty;
    }
}