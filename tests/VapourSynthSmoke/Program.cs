using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Cascade.Models;
using Cascade.Services;
using Cascade.Services.CommandBuilders;
using Cascade.Services.VapourSynth;
using Cascade.ViewModels.Operations;

var directory = Directory.CreateTempSubdirectory("Cascade smoke ");
try
{
    var source = Path.Combine(directory.FullName, "测试 clip & sound.mp4");
    await Run("ffmpeg", "-v", "error", "-f", "lavfi", "-i", "testsrc2=size=160x96:rate=24:duration=2",
        "-f", "lavfi", "-i", "sine=frequency=523:sample_rate=48000:duration=2",
        "-c:v", "libx264", "-preset", "ultrafast", "-c:a", "aac", source);

    var context = OperationContext.Instance;
    context.SelectedBackend = BackendType.VapourSynth;
    context.PublishData("cascade-io-input", "currentInputFile", source);
    const string naming = "cascade-io-naming-output";
    const string encoder = "ffmpeg-video-encoder-strategy";
    const string mux = "ffmpeg-muxing-delivery";
    const string filters = VapourSynthCommandBuilder.PageId;
    context.PublishData(naming, "OutputPath", directory.FullName);
    context.PublishData(naming, "SelectedConflictResolutionIndex", 5);
    context.PublishData(encoder, "SelectedCodecIndex", 1); // H.264, x264
    context.PublishData(encoder, "CrfValue", 18);
    context.PublishData(encoder, "ProfileValue", 2);
    context.PublishData(encoder, "PresetValue", 4);
    context.PublishData(mux, "SelectedContainerIndex", 1); // MP4
    var cleanVideo = "";
    foreach (var (depthIndex, blur, audio) in new[] { (0, 0, "copy"), (0, 3, "none"), (1, 0, "aac") })
    {
        context.PublishData(naming, "OutputFileName", $"{audio}.mp4");
        context.PublishData(filters, "DepthIndex", depthIndex);
        context.PublishData(filters, "BlurRadius", blur);
        context.PublishData(filters, "Threads", blur == 0 ? 0 : 2);
        context.PublishData(mux, "AudioMode", audio);
        context.PublishData(mux, "AacBitrate", 128);
        var command = context.GenerateCommand();
        var job = command.VapourSynthJob ?? throw new Exception(string.Join("\n", command.ValidationResult.Errors));
        await Enqueue(job, QueueTaskStatus.Completed);
        var json = await Run("ffprobe", "-v", "error", "-count_frames", "-show_streams", "-of", "json", job.Destination);
        using var probe = JsonDocument.Parse(json);
        var streams = probe.RootElement.GetProperty("streams");
        var video = streams[0];
        Require(video.GetProperty("nb_read_frames").GetString() == "48" &&
            video.GetProperty("r_frame_rate").GetString() == "24/1", "Frame count or rate changed.");
        Require(video.GetProperty("pix_fmt").GetString() == (depthIndex == 0 ? "yuv420p" : "yuv420p10le"),
            "Unexpected output bit depth.");
        Require(streams.GetArrayLength() == (audio == "none" ? 1 : 2), "Unexpected audio track count.");
        if (audio == "copy")
        {
            Require(await StreamHash(source, "a", "copy") == await StreamHash(job.Destination, "a", "copy"),
                "Compressed audio packets changed in copy mode.");
            cleanVideo = await StreamHash(job.Destination, "v", "rawvideo");
        }
        else if (audio == "none")
        {
            Require(await StreamHash(job.Destination, "v", "rawvideo") != cleanVideo, "Blur did not change pixels.");
        }
        else
        {
            Require(streams[1].GetProperty("codec_name").GetString() == "aac", "Expected AAC audio.");
            var bitrate = int.Parse(streams[1].GetProperty("bit_rate").GetString()!, CultureInfo.InvariantCulture);
            Require(bitrate is > 115000 and < 145000, "AAC stream did not use the requested 128 kb/s target.");
            var before = File.ReadAllBytes(job.Destination);
            await Enqueue(job with { EncoderOptions = "-c:v nonexistent_smoke_encoder" }, QueueTaskStatus.Failed);
            Require(before.SequenceEqual(File.ReadAllBytes(job.Destination)), "Failed job replaced the existing output.");
        }
    }
    context.PublishData(encoder, "SelectedCodecIndex", 0);
    Require(!context.GenerateCommand().ValidationResult.IsValid, "Filtered video accepted stream copy.");
    Require(!directory.EnumerateFiles(".cascade-*").Any(), "Temporary output was left behind.");
    Console.WriteLine("Passed VapourSynth queue, video, audio, and failed-output checks.");
}
finally
{
    directory.Delete(recursive: true);
}

static async Task Enqueue(VapourSynthJob job, QueueTaskStatus expected)
{
    var queue = QueueService.Instance;
    var item = new QueueTask { MediaItem = new MediaItem { Name = Path.GetFileName(job.Source), FilePath = job.Source },
        Command = job.Preview, OutputPath = job.Destination, VapourSynthJob = job };
    var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    void OnStatus(object? sender, QueueTask task)
    {
        if (task.Id == item.Id && task.Status is QueueTaskStatus.Completed or QueueTaskStatus.Failed)
            finished.TrySetResult();
    }
    queue.TaskStatusChanged += OnStatus;
    try
    {
        queue.AddTask(item);
        queue.Start();
        await finished.Task.WaitAsync(TimeSpan.FromMinutes(1));
        Require(item.Status == expected, $"Expected {expected}, got {item.Status}. {item.ErrorMessage}");
    }
    finally
    {
        queue.TaskStatusChanged -= OnStatus;
    }
}

static Task<string> StreamHash(string file, string stream, string codec) =>
    Run("ffmpeg", "-v", "error", "-i", file, "-map", $"0:{stream}:0", "-c", codec, "-f", "hash", "-hash", "sha256", "-");

static async Task<string> Run(string executable, params string[] args)
{
    var info = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
    foreach (var argument in args) info.ArgumentList.Add(argument);
    using var process = Process.Start(info) ?? throw new Exception($"Cannot start {executable}.");
    var output = process.StandardOutput.ReadToEndAsync();
    var errors = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    Require(process.ExitCode == 0, $"{executable} failed. {await errors}");
    return await output;
}

static void Require(bool passed, string message)
{
    if (!passed) throw new InvalidOperationException(message);
}
