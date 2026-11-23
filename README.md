<div align="center">

![Cascade Hero](Designs/CascadeHeroImage.png)

# Cascade

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF-68217A)](https://github.com/dotnet/wpf)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/yourusername/Cascade/pulls)

**飞流多媒体 精彩我世界**

</div>


## VapourSynth 视频帧处理

在 Windows 安装 64 位 [VapourSynth](https://www.vapoursynth.com/doc/installation.html) 和与该运行时匹配的 [FFMS2 源插件](https://github.com/FFMS/ffms2/releases)，让 VapourSynth 能自动加载 FFMS2。将 `vspipe.exe` 和 `ffmpeg.exe` 所在目录加入 PATH，重启 Cascade。应用也会在安装目录的 `Libs` 文件夹查找这两个程序。

1. 导入视频，在「Cascade 输入与输出」中设置输出目录或文件名。
2. 在 FFmpeg 的视频编码器设置中选择重新编码（如 H.264 / x264），再选择输出容器和音频选项。
3. 在「VapourSynth → 视频帧处理」中勾选启用，设置处理位深、模糊半径和线程数。在任务页预览命令并加入队列。

VSPipe 通过 Y4M 向 FFmpeg 传递恒定帧率的视频帧，原文件提供音轨。适用于恒定帧率素材。可变帧率时间戳和 HDR 逐帧元数据无法通过此管道保留。处理位深控制中间帧的精度，最终像素格式由编码器设置决定。

<div align="center">

**[回到顶部](#cascade)**

BEITAware

</div>
