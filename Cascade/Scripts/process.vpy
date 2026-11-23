import vapoursynth as vs

core = vs.core
thread_count = int(threads)
radius = int(blur)
bits = int(depth)
if thread_count:
    core.num_threads = thread_count

if not hasattr(core, "ffms2"):
    raise vs.Error("FFMS2 is required. Install the FFMS2 plugin for your VapourSynth runtime.")
clip = core.ffms2.Source(source=source, cache=False)
if clip.format is None:
    raise vs.Error("The VapourSynth pipeline requires a constant video format.")

# Preserve YUV/gray chroma sampling. Y4M cannot carry RGB; use BT.709 there.
fmt = clip.format
if fmt.color_family == vs.RGB:
    target = vs.YUV444P10 if bits == 10 else vs.YUV444P8
    clip = core.resize.Bicubic(clip, format=target, matrix_s="709")
elif fmt.bits_per_sample != bits or fmt.sample_type != vs.INTEGER:
    target = core.query_video_format(fmt.color_family, vs.INTEGER, bits,
                                     fmt.subsampling_w, fmt.subsampling_h)
    clip = core.resize.Point(clip, format=target.id, dither_type="ordered")

if radius:
    clip = core.std.BoxBlur(clip, hradius=radius, vradius=radius)
clip.set_output()
