using Cascade.ViewModels.Operations;

namespace Cascade.Services.CommandBuilders.FFmpeg.Providers
{
    public class AudioEncoderSegmentProvider : ICommandSegmentProvider
    {
        private const string Page = "ffmpeg-muxing-delivery";
        public CommandSegmentType SegmentType => CommandSegmentType.AudioCodec;
        public int Priority => 0;

        public CommandSegment GenerateSegment(OperationContext context)
        {
            var mode = context.GetData(Page, "AudioMode", "auto");
            var bitrate = context.GetData(Page, "AacBitrate", 192);
            var validation = ValidateConfiguration(context);
            return new CommandSegment
            {
                Type = SegmentType,
                IsValid = validation.IsValid,
                ValidationMessage = string.Join(" ", validation.Errors),
                Parameters = mode switch
                {
                    "copy" => "-c:a copy",
                    "aac" when validation.IsValid => $"-c:a aac -b:a {bitrate}k",
                    "none" => "-an",
                    _ => string.Empty
                }
            };
        }

        public ValidationResult ValidateConfiguration(OperationContext context)
        {
            var result = new ValidationResult();
            var mode = context.GetData(Page, "AudioMode", "auto");
            if (mode is not ("auto" or "copy" or "aac" or "none"))
                result.AddError("Unsupported audio mode.");
            var bitrate = context.GetData(Page, "AacBitrate", 192);
            if (mode == "aac" && bitrate is < 64 or > 320)
                result.AddError("AAC bitrate must be between 64 and 320 kb/s.");
            return result;
        }
    }
}
