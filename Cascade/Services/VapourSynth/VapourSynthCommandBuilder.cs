using System.IO;
using Cascade.Services.CommandBuilders;
using Cascade.Services.CommandBuilders.FFmpeg;
using Cascade.Services.CascadeIO;
using Cascade.ViewModels.Operations;

namespace Cascade.Services.VapourSynth;

public sealed class VapourSynthCommandBuilder : FFmpegCommandBuilder
{
    public const string PageId = "vapoursynth-processing";
    public override BackendType SupportedBackend => BackendType.VapourSynth;
    public override string DisplayName => "VapourSynth";

    protected override CommandResult AssembleCommand(OperationContext context, List<CommandSegment> segments, ValidationResult validation)
    {
        var result = new CommandResult { Backend = SupportedBackend, ValidationResult = validation };
        ValidateProcessing(context, validation);
        if (!validation.IsValid) return result;

        var source = context.GetData<string>("cascade-io-input", "currentInputFile")!;
        var output = segments.Single(s => s.Type == CommandSegmentType.OutputFile).Parameters.Trim('"');
        if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))
        {
            validation.AddError(GetLocalizedString("VS_SourceIsOutput"));
            return result;
        }

        var options = segments.Where(s => s.Type != CommandSegmentType.Input && s.Type != CommandSegmentType.OutputFile);
        result.VapourSynthJob = new VapourSynthJob(source, output, string.Join(" ", options.Select(s => s.Parameters)),
            context.GetData<int>("cascade-io-naming-output", "SelectedConflictResolutionIndex") == (int)ConflictResolution.Overwrite,
            context.GetData<int>(PageId, "BlurRadius"), context.GetData<int>(PageId, "DepthIndex") == 1 ? 10 : 8,
            context.GetData<int>(PageId, "Threads"));
        result.Content = result.VapourSynthJob.Preview;
        return result;
    }

    private static void ValidateProcessing(OperationContext context, ValidationResult result)
    {
        if (context.GetData<int>("ffmpeg-video-encoder-strategy", "SelectedCodecIndex") == 0)
            result.AddError(GetLocalizedString("VS_ChooseEncoder"));
        if (context.GetData<int>(PageId, "BlurRadius") is < 0 or > 8 ||
            context.GetData<int>(PageId, "DepthIndex") is < 0 or > 1 ||
            context.GetData<int>(PageId, "Threads") is < 0 or > 32)
            result.AddError(GetLocalizedString("VS_InvalidSettings"));
    }

    public override ValidationResult ValidateConfiguration(OperationContext context)
    {
        var result = base.ValidateConfiguration(context);
        ValidateProcessing(context, result);
        return result;
    }

    public override string GetPreview(OperationContext context)
    {
        var result = GenerateCommand(context);
        return result.ValidationResult.IsValid ? result.Content : string.Join("\n", result.ValidationResult.Errors);
    }

    public override IReadOnlyList<string> GetSupportedPages() => [.. base.GetSupportedPages(), PageId];
}
