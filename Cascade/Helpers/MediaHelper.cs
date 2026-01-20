using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Cascade.Models;

namespace Cascade.Helpers
{
    public static class MediaHelper
    {
        private static readonly string FfmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "ffmpeg.exe");
        private static readonly string FfprobePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "ffprobe.exe");

        // 限制并发数量
        // 缩略图生成（CPU密集型）：最大线程数量为逻辑处理器数量
        private static readonly SemaphoreSlim ThumbnailSemaphore = new SemaphoreSlim(Environment.ProcessorCount);
        
        // Probe（IO/轻量级CPU）：最大进程数量是逻辑处理器数量的一半
        private static readonly SemaphoreSlim ProbeSemaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2));

        public static async Task LoadMediaInfoAsync(MediaItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath)) return;

            item.IsLoading = true;

            try
            {
                // 并行执行缩略图生成和信息探测
                var thumbnailTask = GenerateThumbnailAsync(item);
                var probeTask = ProbeMediaInfoAsync(item);

                await Task.WhenAll(thumbnailTask, probeTask);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载媒体信息失败: {ex.Message}");
            }
            finally
            {
                // 确保在 UI 线程更新 IsLoading
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    item.IsLoading = false;
                });
            }
        }

        private static async Task GenerateThumbnailAsync(MediaItem item)
        {
            await ThumbnailSemaphore.WaitAsync();
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"hyrt_{Guid.NewGuid()}.jpg");
                
                // 检查 FFmpeg 是否存在
                if (!File.Exists(FfmpegPath))
                {
                    Debug.WriteLine($"FFmpeg 不存在: {FfmpegPath}");
                    return;
                }
                
                // 使用 ffmpeg 生成缩略图
                // -ss 0 : 从开头截取（避免视频太短导致失败）
                // -i : 输入文件
                // -vframes 1 : 输出1帧
                // -vf scale='min(320,iw)':-1 : 保持比例缩放，宽度最大320p
                // -q:v 2 : 图片质量
                var startInfo = new ProcessStartInfo
                {
                    FileName = FfmpegPath,
                    Arguments = $"-ss 0 -i \"{item.FilePath}\" -vframes 1 -vf \"scale='min(320,iw)':-1\" -q:v 2 \"{tempFile}\" -y",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        // 必须异步读取 stdout 和 stderr，否则可能造成死锁
                        var stderrTask = process.StandardError.ReadToEndAsync();
                        var stdoutTask = process.StandardOutput.ReadToEndAsync();
                        
                        // 设置超时（30秒）
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        try
                        {
                            await process.WaitForExitAsync(cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine($"FFmpeg 超时，正在终止进程: {item.FilePath}");
                            try { process.Kill(true); } catch { }
                            return;
                        }
                        
                        // 读取输出用于调试
                        string stderr = await stderrTask;
                        if (process.ExitCode != 0)
                        {
                            Debug.WriteLine($"FFmpeg 退出码: {process.ExitCode}, stderr: {stderr}");
                        }
                    }
                }

                if (File.Exists(tempFile))
                {
                    try
                    {
                        // 加载图片到内存，避免文件占用
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(tempFile);
                        bitmap.EndInit();
                        bitmap.Freeze(); // 使其可跨线程访问

                        // 更新 UI 需要在主线程
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            item.Thumbnail = bitmap;
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"加载缩略图图片失败: {ex.Message}");
                    }

                    // 删除临时文件
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch { /* 忽略删除失败 */ }
                }
                else
                {
                    Debug.WriteLine($"缩略图文件未生成: {tempFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"生成缩略图失败: {ex.Message}");
            }
            finally
            {
                ThumbnailSemaphore.Release();
            }
        }

        private static async Task ProbeMediaInfoAsync(MediaItem item)
        {
            await ProbeSemaphore.WaitAsync();
            Debug.WriteLine($"[Probe] 开始探测: {item.FilePath}");
            try
            {
                // 检查 ffprobe 是否存在
                if (!File.Exists(FfprobePath))
                {
                    Debug.WriteLine($"[Probe] ffprobe 不存在: {FfprobePath}");
                    return;
                }
                Debug.WriteLine($"[Probe] ffprobe 路径: {FfprobePath}");

                // 使用 ffprobe 获取 JSON 格式的媒体信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = FfprobePath,
                    Arguments = $"-v quiet -print_format json -show_format -show_streams \"{item.FilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Debug.WriteLine($"[Probe] 命令参数: {startInfo.Arguments}");

                string jsonOutput = "";
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        Debug.WriteLine($"[Probe] 进程已启动, PID: {process.Id}");
                        
                        // 必须异步读取 stdout 和 stderr，否则可能造成死锁
                        var stderrTask = process.StandardError.ReadToEndAsync();
                        var stdoutTask = process.StandardOutput.ReadToEndAsync();

                        // 设置超时（30秒）
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        try
                        {
                            await process.WaitForExitAsync(cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine($"[Probe] ffprobe 超时，正在终止进程: {item.FilePath}");
                            try { process.Kill(true); } catch { }
                            return;
                        }

                        jsonOutput = await stdoutTask;
                        string stderr = await stderrTask;

                        Debug.WriteLine($"[Probe] 进程退出码: {process.ExitCode}");
                        Debug.WriteLine($"[Probe] stdout 长度: {jsonOutput.Length}");
                        Debug.WriteLine($"[Probe] stderr 长度: {stderr.Length}");
                        
                        if (process.ExitCode != 0)
                        {
                            Debug.WriteLine($"[Probe] ffprobe 错误, stderr: {stderr}");
                        }
                        
                        // 输出 JSON 的前 500 个字符用于调试
                        if (jsonOutput.Length > 0)
                        {
                            Debug.WriteLine($"[Probe] JSON 预览: {jsonOutput.Substring(0, Math.Min(500, jsonOutput.Length))}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[Probe] 进程启动失败!");
                    }
                }

                if (!string.IsNullOrEmpty(jsonOutput))
                {
                    Debug.WriteLine($"[Probe] 开始解析 JSON...");
                    // 在 UI 线程更新属性，确保数据绑定通知正常工作
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ParseProbeJson(item, jsonOutput);
                    });
                    Debug.WriteLine($"[Probe] JSON 解析完成");
                }
                else
                {
                    Debug.WriteLine($"[Probe] ffprobe 未返回数据: {item.FilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Probe] 探测媒体信息失败: {ex.Message}");
                Debug.WriteLine($"[Probe] 异常堆栈: {ex.StackTrace}");
            }
            finally
            {
                ProbeSemaphore.Release();
                Debug.WriteLine($"[Probe] 探测结束: {item.FilePath}");
            }
        }

        private static void ParseProbeJson(MediaItem item, string json)
        {
            Debug.WriteLine($"[ParseProbeJson] 开始解析, JSON长度: {json.Length}");
            try
            {
                // 清理 JSON 中的无效转义序列
                // ffprobe 有时会在元数据中包含 \0 等无效的 JSON 转义字符
                json = CleanJsonString(json);
                Debug.WriteLine($"[ParseProbeJson] 清理后 JSON 长度: {json.Length}");
                
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    Debug.WriteLine($"[ParseProbeJson] JSON 解析成功, 根元素类型: {root.ValueKind}");
                    
                    var metadataList = new System.Collections.Generic.List<string>();
                    var subtitleList = new System.Collections.Generic.List<string>();
                    
                    // 保存容器级别的比特率，用于在流级别没有比特率时作为后备
                    long containerBitrate = 0;

                    // 解析 Format 信息
                    if (root.TryGetProperty("format", out var formatElement))
                    {
                        Debug.WriteLine($"[ParseProbeJson] 找到 format 节点");
                        
                        if (formatElement.TryGetProperty("duration", out var durationProp))
                        {
                            Debug.WriteLine($"[ParseProbeJson] duration 原始值: {durationProp}, 类型: {durationProp.ValueKind}");
                            string durationStr = durationProp.ValueKind == JsonValueKind.Number ? durationProp.ToString() : (durationProp.GetString() ?? "");
                            Debug.WriteLine($"[ParseProbeJson] duration 字符串: {durationStr}");
                            if (double.TryParse(durationStr, out double durationSeconds))
                            {
                                TimeSpan ts = TimeSpan.FromSeconds(durationSeconds);
                                item.Duration = ts.ToString(@"hh\:mm\:ss");
                                Debug.WriteLine($"[ParseProbeJson] 设置 Duration = {item.Duration}");
                            }
                            else
                            {
                                Debug.WriteLine($"[ParseProbeJson] duration 解析失败: {durationStr}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[ParseProbeJson] 未找到 duration 属性");
                        }
                        
                        if (formatElement.TryGetProperty("bit_rate", out var bitrateProp))
                        {
                            Debug.WriteLine($"[ParseProbeJson] bit_rate 原始值: {bitrateProp}, 类型: {bitrateProp.ValueKind}");
                            string bitrateStr = bitrateProp.ValueKind == JsonValueKind.Number ? bitrateProp.ToString() : (bitrateProp.GetString() ?? "");
                            if (long.TryParse(bitrateStr, out long bps))
                            {
                                containerBitrate = bps;
                                item.Bitrate = $"{bps / 1000} kbps";
                                Debug.WriteLine($"[ParseProbeJson] 设置 Bitrate = {item.Bitrate}, 容器比特率 = {containerBitrate}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[ParseProbeJson] 未找到 bit_rate 属性");
                        }

                        // Format Metadata
                        if (formatElement.TryGetProperty("tags", out var tagsElement))
                        {
                            foreach (var tag in tagsElement.EnumerateObject())
                            {
                                metadataList.Add($"[容器] {tag.Name}: {tag.Value}");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[ParseProbeJson] 未找到 format 节点!");
                    }

                    // 解析 Streams 信息
                    if (root.TryGetProperty("streams", out var streamsElement))
                    {
                        int videoCount = 0;
                        int audioCount = 0;
                        int subtitleCount = 0;

                        foreach (var stream in streamsElement.EnumerateArray())
                        {
                            if (stream.TryGetProperty("codec_type", out var typeProp))
                            {
                                string type = typeProp.GetString() ?? "";
                                
                                if (type == "video")
                                {
                                    videoCount++;
                                    Debug.WriteLine($"[ParseProbeJson] 发现视频流 #{videoCount}");
                                    
                                    // 输出视频流的所有字段用于调试
                                    Debug.WriteLine($"[ParseProbeJson] 视频流字段列表:");
                                    foreach (var prop in stream.EnumerateObject())
                                    {
                                        Debug.WriteLine($"[ParseProbeJson]   - {prop.Name}: {prop.Value}");
                                    }
                                    
                                    // 只取第一个视频流的主要信息，或者如果有多个视频流，可以考虑如何显示
                                    // 这里假设主要关注第一个视频流
                                    if (videoCount == 1)
                                    {
                                        if (stream.TryGetProperty("codec_name", out var codecProp))
                                        {
                                            item.VideoCodec = codecProp.GetString() ?? "";
                                            Debug.WriteLine($"[ParseProbeJson] 设置 VideoCodec = {item.VideoCodec}");
                                        }

                                        if (stream.TryGetProperty("profile", out var profileProp))
                                        {
                                            item.VideoProfile = profileProp.GetString() ?? "";
                                        }

                                        if (stream.TryGetProperty("level", out var levelProp))
                                        {
                                            item.VideoLevel = levelProp.ValueKind == JsonValueKind.Number ? levelProp.ToString() : (levelProp.GetString() ?? "");
                                        }

                                        if (stream.TryGetProperty("width", out var widthProp) &&
                                            stream.TryGetProperty("height", out var heightProp))
                                        {
                                            int w = 0, h = 0;
                                            if (widthProp.ValueKind == JsonValueKind.Number) w = widthProp.GetInt32();
                                            else int.TryParse(widthProp.GetString(), out w);
                                            
                                            if (heightProp.ValueKind == JsonValueKind.Number) h = heightProp.GetInt32();
                                            else int.TryParse(heightProp.GetString(), out h);

                                            item.Resolution = $"{w}x{h}";
                                        }

                                        // 色深 (Pix Fmt / Bits Per Raw Sample)
                                        string pixFmt = "";
                                        if (stream.TryGetProperty("pix_fmt", out var pixFmtProp))
                                        {
                                            pixFmt = pixFmtProp.GetString() ?? "";
                                        }
                                        
                                        string bitsPerRawSample = "";
                                        if (stream.TryGetProperty("bits_per_raw_sample", out var bitsProp))
                                        {
                                            bitsPerRawSample = bitsProp.ValueKind == JsonValueKind.Number ? bitsProp.ToString() : (bitsProp.GetString() ?? "");
                                        }

                                        if (!string.IsNullOrEmpty(bitsPerRawSample) && bitsPerRawSample != "8")
                                        {
                                            item.VideoColorDepth = $"{bitsPerRawSample}-bit ({pixFmt})";
                                        }
                                        else
                                        {
                                            item.VideoColorDepth = string.IsNullOrEmpty(pixFmt) ? "8-bit" : $"8-bit ({pixFmt})";
                                        }

                                        if (stream.TryGetProperty("r_frame_rate", out var fpsProp))
                                        {
                                            // fps 可能是 "30/1" 这种格式
                                            string fpsStr = fpsProp.ValueKind == JsonValueKind.Number ? fpsProp.ToString() : (fpsProp.GetString() ?? "");
                                            if (fpsStr.Contains("/"))
                                            {
                                                var parts = fpsStr.Split('/');
                                                if (parts.Length == 2 &&
                                                    double.TryParse(parts[0], out double num) &&
                                                    double.TryParse(parts[1], out double den) && den != 0)
                                                {
                                                    item.FrameRate = $"{num / den:F2} fps";
                                                }
                                            }
                                            else
                                            {
                                                item.FrameRate = $"{fpsStr} fps";
                                            }
                                        }

                                        // 尝试获取视频流比特率
                                        if (stream.TryGetProperty("bit_rate", out var vBitrateProp))
                                        {
                                            string vBitrateStr = vBitrateProp.ValueKind == JsonValueKind.Number ? vBitrateProp.ToString() : (vBitrateProp.GetString() ?? "");
                                            if (long.TryParse(vBitrateStr, out long vBps))
                                            {
                                                item.VideoBitrate = $"{vBps / 1000} kbps";
                                                Debug.WriteLine($"[ParseProbeJson] 设置 VideoBitrate = {item.VideoBitrate} (来自流)");
                                            }
                                        }
                                        else if (containerBitrate > 0)
                                        {
                                            // MTS/AVCHD 等格式可能不在流级别提供比特率
                                            // 使用容器比特率作为视频比特率的近似值（标注为估算值）
                                            item.VideoBitrate = $"~{containerBitrate / 1000} kbps";
                                            Debug.WriteLine($"[ParseProbeJson] 设置 VideoBitrate = {item.VideoBitrate} (估算自容器)");
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"[ParseProbeJson] 无法获取视频比特率");
                                        }
                                    }

                                    // Video Metadata
                                    if (stream.TryGetProperty("tags", out var tagsElement))
                                    {
                                        foreach (var tag in tagsElement.EnumerateObject())
                                        {
                                            metadataList.Add($"[视频流#{videoCount}] {tag.Name}: {tag.Value}");
                                        }
                                    }
                                }
                                else if (type == "audio")
                                {
                                    audioCount++;
                                    // 同样，主要关注第一个音频流，或者拼接信息
                                    if (audioCount == 1)
                                    {
                                        if (stream.TryGetProperty("codec_name", out var codecProp))
                                        {
                                            item.AudioCodec = codecProp.GetString() ?? "";
                                        }

                                        if (stream.TryGetProperty("sample_rate", out var sampleRateProp))
                                        {
                                            string sampleRateStr = sampleRateProp.ValueKind == JsonValueKind.Number ? sampleRateProp.ToString() : (sampleRateProp.GetString() ?? "");
                                            if (int.TryParse(sampleRateStr, out int sampleRate))
                                            {
                                                item.AudioSampleRate = $"{sampleRate} Hz";
                                            }
                                            else
                                            {
                                                item.AudioSampleRate = sampleRateStr;
                                            }
                                        }

                                        if (stream.TryGetProperty("bits_per_sample", out var bitsProp))
                                        {
                                            int bits = 0;
                                            if (bitsProp.ValueKind == JsonValueKind.Number) bits = bitsProp.GetInt32();
                                            else int.TryParse(bitsProp.GetString(), out bits);

                                            if (bits > 0)
                                            {
                                                item.AudioBitDepth = $"{bits}-bit";
                                            }
                                        }
                                        // 如果 bits_per_sample 为 0，尝试从 sample_fmt 推断 (例如 fltp 通常是 32-bit float)
                                        if (string.IsNullOrEmpty(item.AudioBitDepth) && stream.TryGetProperty("sample_fmt", out var fmtProp))
                                        {
                                            item.AudioBitDepth = fmtProp.GetString() ?? "";
                                        }

                                        if (stream.TryGetProperty("bit_rate", out var aBitrateProp))
                                        {
                                            string aBitrateStr = aBitrateProp.ValueKind == JsonValueKind.Number ? aBitrateProp.ToString() : (aBitrateProp.GetString() ?? "");
                                            if (long.TryParse(aBitrateStr, out long aBps))
                                            {
                                                item.AudioBitrate = $"{aBps / 1000} kbps";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 如果有多个音轨，可以追加显示，或者只在元数据里显示
                                        if (stream.TryGetProperty("codec_name", out var codecProp))
                                        {
                                            string codec = codecProp.GetString() ?? "";
                                            if (!item.AudioCodec.Contains(codec))
                                            {
                                                item.AudioCodec += $", {codec}";
                                            }
                                        }
                                    }

                                    // Audio Metadata
                                    if (stream.TryGetProperty("tags", out var tagsElement))
                                    {
                                        foreach (var tag in tagsElement.EnumerateObject())
                                        {
                                            metadataList.Add($"[音频流#{audioCount}] {tag.Name}: {tag.Value}");
                                        }
                                    }
                                }
                                else if (type == "subtitle")
                                {
                                    subtitleCount++;
                                    string subInfo = $"#{subtitleCount}";
                                    
                                    if (stream.TryGetProperty("codec_name", out var codecProp))
                                    {
                                        subInfo += $" {codecProp.GetString()}";
                                    }

                                    if (stream.TryGetProperty("tags", out var tagsElement))
                                    {
                                        if (tagsElement.TryGetProperty("language", out var langProp))
                                        {
                                            subInfo += $" ({langProp.GetString()})";
                                        }
                                        if (tagsElement.TryGetProperty("title", out var titleProp))
                                        {
                                            subInfo += $" - {titleProp.GetString()}";
                                        }
                                        
                                        // Subtitle Metadata
                                        foreach (var tag in tagsElement.EnumerateObject())
                                        {
                                            metadataList.Add($"[字幕流#{subtitleCount}] {tag.Name}: {tag.Value}");
                                        }
                                    }
                                    
                                    subtitleList.Add(subInfo);
                                }
                            }
                        }
                    }

                    item.Subtitles = subtitleList.Count > 0 ? string.Join(Environment.NewLine, subtitleList) : "无";
                    item.Metadata = metadataList.Count > 0 ? string.Join(Environment.NewLine, metadataList) : "无";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析 Probe JSON 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理 JSON 字符串中的无效转义序列
        /// ffprobe 的元数据中可能包含如 \0 这样在标准 JSON 中无效的转义字符
        /// </summary>
        /// <param name="json">原始 JSON 字符串</param>
        /// <returns>清理后的 JSON 字符串</returns>
        private static string CleanJsonString(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            // 使用 StringBuilder 进行高效的字符串处理
            var sb = new System.Text.StringBuilder(json.Length);
            bool inString = false;
            bool escape = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escape)
                {
                    // 当前字符是转义字符后的字符
                    // 有效的 JSON 转义字符: " \ / b f n r t u
                    if (c == '"' || c == '\\' || c == '/' || c == 'b' || c == 'f' || c == 'n' || c == 'r' || c == 't' || c == 'u')
                    {
                        sb.Append('\\');
                        sb.Append(c);
                    }
                    else
                    {
                        // 无效的转义序列，跳过反斜杠，只保留字符本身
                        // 例如 \0 变成 0，或者直接跳过
                        // 这里选择跳过整个无效转义序列
                        Debug.WriteLine($"[CleanJsonString] 跳过无效转义序列: \\{c}");
                    }
                    escape = false;
                }
                else if (c == '\\' && inString)
                {
                    // 在字符串内遇到反斜杠，标记进入转义状态
                    escape = true;
                }
                else
                {
                    if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    {
                        inString = !inString;
                    }
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
