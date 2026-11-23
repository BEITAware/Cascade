using System.Collections.Concurrent;
using Cascade.Models;
using Cascade.Services;

// Run with dotnet run --project tests/QueueControls. No encoder or media fixture is needed.
var service = QueueService.Instance;
service.MaxConcurrentTasks = 1;
var started = new ConcurrentQueue<string>();
using var gate = new ManualResetEventSlim();
var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var firstEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var timeout = TimeSpan.FromSeconds(10);
var marker = Path.GetTempFileName();
QueueTask Job(string name) => new()
{
    MediaItem = new MediaItem { Name = name },
    // The scheduler is under test. An existing file cannot be a directory, so execution
    // fails deterministically before starting FFmpeg, exercising advancement after failure.
    OutputPath = Path.Combine(marker, "out.mkv"),
    Command = "ffmpeg -version"
};
var first = Job("first");
var second = Job("second");
var last = Job("last");
var removed = Job("removed");

service.TaskStatusChanged += (_, task) =>
{
    if (task.Status == QueueTaskStatus.Processing)
    {
        started.Enqueue(task.MediaItem.Name);
        if (task == last)
        {
            firstStarted.TrySetResult();
            gate.Wait(timeout);
        }
    }
    if (task.Status == QueueTaskStatus.Failed)
    {
        if (task == last) firstEnded.TrySetResult();
        if (task == second) drained.TrySetResult();
    }
};

try
{
    service.AddTasks([removed, first, second, last]);
    if (!service.RemoveTask(removed.Id) || !service.MoveTaskUp(last.Id) || !service.MoveTaskUp(last.Id))
        throw new Exception("Could not arrange waiting jobs");
    service.Start();
    await firstStarted.Task.WaitAsync(timeout);
    service.Pause();
    gate.Set();
    await firstEnded.Task.WaitAsync(timeout);
    if (!started.SequenceEqual(["last"]) || first.Status != QueueTaskStatus.Waiting)
        throw new Exception("Cancelled, reordered or paused jobs ran unexpectedly");
    for (int i = 0; i < 4; i++) { service.Start(); service.Pause(); }
    service.Start();
    await drained.Task.WaitAsync(timeout);
    if (!started.SequenceEqual(["last", "first", "second"]))
        throw new Exception($"Unexpected dispatch order: {string.Join(", ", started)}");
    Console.WriteLine("PASS: removal, ordering, pause/resume, concurrency limit and failure advancement");
}
finally
{
    gate.Set();
    service.Pause();
    File.Delete(marker);
}
