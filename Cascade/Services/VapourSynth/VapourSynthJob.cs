using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Cascade.Services.VapourSynth;

/// <summary>The settings and paths of one queued encode, captured before execution.</summary>
public sealed record VapourSynthJob(
    string Source, string Destination, string EncoderOptions, bool Overwrite,
    int BlurRadius, int WorkingDepth, int Threads)
{
    private static string ScriptPath => Path.Combine(AppContext.BaseDirectory, "Scripts", "process.vpy");

    private IEnumerable<string> ProducerArguments =>
        ["--arg", $"source={Source}", "--arg", $"blur={BlurRadius}", "--arg", $"depth={WorkingDepth}",
         "--arg", $"threads={Threads}", "-c", "y4m", ScriptPath, "-"];

    private string EncoderArguments(string output) =>
        $"-nostdin -f yuv4mpegpipe -i pipe:0 -i \"{Source}\" -map 0:v:0 -map 1:a? {EncoderOptions} \"{output}\"";

    private static string QuoteForDisplay(string value) =>
        value.Any(c => char.IsWhiteSpace(c) || "&|()<>^".Contains(c)) ? $"\"{value}\"" : value;

    public string Preview =>
        QuoteForDisplay(FindTool("vspipe")) + " " + string.Join(" ", ProducerArguments.Select(QuoteForDisplay)) +
        " | " + QuoteForDisplay(FindTool("ffmpeg")) + (Overwrite ? " -y " : " -n ") + EncoderArguments(Destination);

    public async Task RunAsync(Action<string> reportProgress, CancellationToken cancellationToken)
    {
        if (string.Equals(Path.GetFullPath(Source), Path.GetFullPath(Destination), StringComparison.OrdinalIgnoreCase))
            throw new IOException(LocalizationService.GetString("VS_SourceIsOutput"));
        if (File.Exists(Destination) && !Overwrite)
            throw new IOException($"{LocalizationService.GetString("VS_OutputExists")} {Destination}");

        var directory = Path.GetDirectoryName(Path.GetFullPath(Destination))!;
        Directory.CreateDirectory(directory);
        string temporaryOutput = Path.Combine(directory, $".cascade-{Guid.NewGuid():N}{Path.GetExtension(Destination)}");
        cancellationToken.ThrowIfCancellationRequested();

        using var producer = CreateProcess("vspipe");
        foreach (var argument in ProducerArguments)
            producer.StartInfo.ArgumentList.Add(argument);
        producer.StartInfo.RedirectStandardOutput = true;

        using var encoder = CreateProcess("ffmpeg");
        encoder.StartInfo.Arguments = "-n " + EncoderArguments(temporaryOutput);
        encoder.StartInfo.RedirectStandardInput = true;

        try
        {
            Start(producer);
            var producerErrors = ReadDiagnosticsAsync(producer, null);
            Start(encoder);
            var encoderErrors = ReadDiagnosticsAsync(encoder, reportProgress);
            using var cancellation = cancellationToken.Register(() => { Stop(producer); Stop(encoder); });

            var encoderExit = encoder.WaitForExitAsync();
            var transfer = TransferAsync(producer, encoder);
            // An encoder can reject its options before the source has finished indexing.
            if (await Task.WhenAny(transfer, encoderExit) == encoderExit && !transfer.IsCompleted)
                Stop(producer);
            Exception? transferError = null;
            try
            {
                await transfer;
            }
            catch (IOException ex)
            {
                transferError = ex;
                Stop(producer);
            }

            await Task.WhenAll(producer.WaitForExitAsync(), encoderExit, producerErrors, encoderErrors);
            cancellationToken.ThrowIfCancellationRequested();
            if (producer.ExitCode != 0 || encoder.ExitCode != 0 || transferError != null)
                throw new IOException($"VSPipe ({producer.ExitCode}): {await producerErrors}\nFFmpeg ({encoder.ExitCode}): {await encoderErrors}", transferError);

            File.Move(temporaryOutput, Destination, Overwrite);
        }
        finally
        {
            Stop(producer);
            Stop(encoder);
            File.Delete(temporaryOutput);
        }
    }

    private static async Task TransferAsync(Process producer, Process encoder)
    {
        try
        {
            await producer.StandardOutput.BaseStream.CopyToAsync(encoder.StandardInput.BaseStream);
        }
        finally
        {
            encoder.StandardInput.Close();
        }
    }

    private static string FindTool(string name)
    {
        // Prefer tools shipped beside the application, then allow a system installation.
        var executable = OperatingSystem.IsWindows() ? name + ".exe" : name;
        var bundled = Path.Combine(AppContext.BaseDirectory, "Libs", executable);
        return File.Exists(bundled) ? bundled : executable;
    }

    private static Process CreateProcess(string name) => new()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = FindTool(name), UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardError = true, StandardErrorEncoding = Encoding.UTF8
        }
    };

    private static void Start(Process process)
    {
        try { process.Start(); }
        catch (Win32Exception ex)
        {
            throw new IOException($"{process.StartInfo.FileName}: {LocalizationService.GetString("VS_ToolError")} ({ex.Message})", ex);
        }
    }

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
        }
        catch (InvalidOperationException) { } // Never started or already exited.
    }

    private static async Task<string> ReadDiagnosticsAsync(Process process, Action<string>? reportProgress)
    {
        var tail = new Queue<string>();
        while (await process.StandardError.ReadLineAsync() is string line)
        {
            reportProgress?.Invoke(line);
            if (tail.Count == 12) tail.Dequeue();
            tail.Enqueue(line);
        }
        return string.Join("\n", tail);
    }
}
