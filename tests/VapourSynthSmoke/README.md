# VapourSynth queue smoke test

Install .NET 10 SDK, FFmpeg with libx264 (8- and 10-bit encoding), ffprobe, and VapourSynth with the FFMS2 source plugin. Put `ffmpeg`, `ffprobe`, and `vspipe` on PATH. From the repository root run

```sh
dotnet run --project tests/VapourSynthSmoke
```

The test generates a two-second clip in a private temporary directory, runs the application's command builder and queue, and checks decoded frames, bit depth, audio modes and preservation of an existing file after an encoder failure. Windows uses the application project. macOS and Linux compile the same backend sources with headless equivalents for the WPF thumbnail type and resource lookup.
