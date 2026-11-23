using System.Text.Json.Nodes;
using Cascade.Services.CommandBuilders;
using Cascade.Services.VapourSynth;

namespace Cascade.ViewModels.Operations.VapourSynth;

public sealed class ProcessingViewModel : OperationViewModelBase
{
    public override string SubPageId => VapourSynthCommandBuilder.PageId;

    public bool UseVapourSynth
    {
        get => Context.SelectedBackend == BackendType.VapourSynth;
        set => Context.SelectedBackend = value ? BackendType.VapourSynth : BackendType.FFmpeg;
    }

    public int BlurRadius
    {
        get => Context.GetData<int>(SubPageId, nameof(BlurRadius));
        set => SetOption(nameof(BlurRadius), value);
    }

    public int DepthIndex
    {
        get => Context.GetData<int>(SubPageId, nameof(DepthIndex));
        set => SetOption(nameof(DepthIndex), value);
    }

    public int Threads
    {
        get => Context.GetData<int>(SubPageId, nameof(Threads));
        set => SetOption(nameof(Threads), value);
    }

    public ProcessingViewModel() => Context.BackendChanged += OnBackendChanged;

    private void OnBackendChanged(object? sender, BackendType backend) => OnPropertyChanged(nameof(UseVapourSynth));

    private void SetOption(string name, int value)
    {
        if (Context.GetData<int>(SubPageId, name) == value) return;
        PublishData(name, value);
        OnPropertyChanged(name);
    }

    protected override void LoadFromContext()
    {
        OnPropertyChanged(nameof(BlurRadius));
        OnPropertyChanged(nameof(DepthIndex));
        OnPropertyChanged(nameof(Threads));
    }

    public override JsonObject Serialize() => new()
    {
        ["enabled"] = UseVapourSynth, ["blurRadius"] = BlurRadius, ["depthIndex"] = DepthIndex, ["threads"] = Threads
    };

    public override void Deserialize(JsonObject data)
    {
        UseVapourSynth = data["enabled"]?.GetValue<bool>() ?? false;
        BlurRadius = data["blurRadius"]?.GetValue<int>() ?? 0;
        DepthIndex = data["depthIndex"]?.GetValue<int>() ?? 0;
        Threads = data["threads"]?.GetValue<int>() ?? 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Context.BackendChanged -= OnBackendChanged;
        base.Dispose(disposing);
    }
}
