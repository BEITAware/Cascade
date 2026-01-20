using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Cascade.ViewModels.Operations;
using Cascade.Services;

namespace Cascade.ViewModels.Operations.FFmpeg
{
    /// <summary>
    /// 编码器（视频）（策略）子页面ViewModel
    /// </summary>
    public class VideoEncoderStrategyViewModel : OperationViewModelBase
    {
        public override string SubPageId => "ffmpeg-video-encoder-strategy";

        #region 编码格式与编码器

        private int _selectedCodecIndex;
        public int SelectedCodecIndex
        {
            get => _selectedCodecIndex;
            set
            {
                if (SetProperty(ref _selectedCodecIndex, value))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[VideoEncoder] SelectedCodecIndex changed to: {value}");
#endif
                    UpdateEncoderList();
                    UpdateVisibility();
                }
            }
        }

        private int _selectedEncoderIndex;
        public int SelectedEncoderIndex
        {
            get => _selectedEncoderIndex;
            set
            {
                if (SetProperty(ref _selectedEncoderIndex, value))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[VideoEncoder] SelectedEncoderIndex changed to: {value}, Encoder: {GetCurrentEncoderName()}");
#endif
                    UpdateEncoderDependentOptions();
                }
            }
        }

        public ObservableCollection<string> AvailableEncoders { get; } = new();

        public bool ShowEncoderSelector => SelectedCodecIndex != 0; // 非复制流时显示

        #endregion

        #region 预设

        private int _presetValue;
        public int PresetValue
        {
            get => _presetValue;
            set
            {
                if (SetProperty(ref _presetValue, value))
                    OnPropertyChanged(nameof(CurrentPresetLabel));
            }
        }

        private int _presetMaxValue = 9;
        public int PresetMaxValue
        {
            get => _presetMaxValue;
            set => SetProperty(ref _presetMaxValue, value);
        }

        private string _presetMinLabel = string.Empty;
        public string PresetMinLabel
        {
            get => _presetMinLabel;
            set => SetProperty(ref _presetMinLabel, value);
        }

        private string _presetMaxLabel = string.Empty;
        public string PresetMaxLabel
        {
            get => _presetMaxLabel;
            set => SetProperty(ref _presetMaxLabel, value);
        }

        public string CurrentPresetLabel => GetCurrentPresetLabel();

        public bool ShowPresetSlider => SelectedCodecIndex != 0 && SelectedCodecIndex != 7; // 非复制流和无压缩

        #endregion

        #region 微调

        private int _selectedTuneIndex;
        public int SelectedTuneIndex
        {
            get => _selectedTuneIndex;
            set => SetProperty(ref _selectedTuneIndex, value);
        }

        public ObservableCollection<string> AvailableTunes { get; } = new();

        public bool ShowTuneSelector => IsX264OrX265Encoder();

        #endregion

        #region 配置文件(Profile)

        private int _profileValue;
        public int ProfileValue
        {
            get => _profileValue;
            set
            {
                if (SetProperty(ref _profileValue, value))
                    OnPropertyChanged(nameof(CurrentProfileLabel));
            }
        }

        private int _profileMaxValue = 2;
        public int ProfileMaxValue
        {
            get => _profileMaxValue;
            set => SetProperty(ref _profileMaxValue, value);
        }

        public string CurrentProfileLabel => GetCurrentProfileLabel();

        public bool ShowProfileSlider => SelectedCodecIndex >= 1 && SelectedCodecIndex <= 2; // H.264/H.265 only (libvvenc不支持profile)

        #endregion

        #region 像素格式

        private int _selectedPixelFormatIndex;
        public int SelectedPixelFormatIndex
        {
            get => _selectedPixelFormatIndex;
            set => SetProperty(ref _selectedPixelFormatIndex, value);
        }

        public ObservableCollection<string> AvailablePixelFormats { get; } = new();

        public bool ShowPixelFormat => SelectedCodecIndex != 0;

        #endregion

        #region 等级(Level)

        private int _levelValue;
        public int LevelValue
        {
            get => _levelValue;
            set
            {
                if (SetProperty(ref _levelValue, value))
                    OnPropertyChanged(nameof(CurrentLevelLabel));
            }
        }

        private int _levelMaxValue = 15;
        public int LevelMaxValue
        {
            get => _levelMaxValue;
            set => SetProperty(ref _levelMaxValue, value);
        }

        private string _levelMaxLabel = "6.2";
        public string LevelMaxLabel
        {
            get => _levelMaxLabel;
            set => SetProperty(ref _levelMaxLabel, value);
        }

        public string CurrentLevelLabel => GetCurrentLevelLabel();

        public bool ShowLevelSlider => SelectedCodecIndex >= 1 && SelectedCodecIndex <= 3;

        #endregion

        #region 码率与质量

        private int _selectedRateModeIndex;
        public int SelectedRateModeIndex
        {
            get => _selectedRateModeIndex;
            set
            {
                if (SetProperty(ref _selectedRateModeIndex, value))
                {
                    OnPropertyChanged(nameof(IsCrfMode));
                    OnPropertyChanged(nameof(IsBitrateMode));
                    OnPropertyChanged(nameof(IsCqpMode));
                }
            }
        }

        public bool ShowRateControl => SelectedCodecIndex != 0; // 非复制流

        public bool IsCrfMode => SelectedRateModeIndex == 0;
        public bool IsBitrateMode => SelectedRateModeIndex == 1 || SelectedRateModeIndex == 2 || SelectedRateModeIndex == 3;
        public bool IsCqpMode => SelectedRateModeIndex == 4;

        private int _crfValue = 23;
        public int CrfValue
        {
            get => _crfValue;
            set => SetProperty(ref _crfValue, value);
        }

        private int _defaultCrfValue = 23;
        public int DefaultCrfValue
        {
            get => _defaultCrfValue;
            set => SetProperty(ref _defaultCrfValue, value);
        }

        private int _targetBitrate = 5000;
        public int TargetBitrate
        {
            get => _targetBitrate;
            set => SetProperty(ref _targetBitrate, value);
        }

        private int _qpI = 20;
        public int QpI
        {
            get => _qpI;
            set => SetProperty(ref _qpI, value);
        }

        private int _qpP = 23;
        public int QpP
        {
            get => _qpP;
            set => SetProperty(ref _qpP, value);
        }

        private int _qpB = 26;
        public int QpB
        {
            get => _qpB;
            set => SetProperty(ref _qpB, value);
        }

        #endregion

        #region 预设标签数组

        private static readonly string[] H264Profiles = { "Baseline", "Main", "High" };
        private static readonly string[] H265Profiles = { "Main", "Main 10", "Main 12", "Main 4:2:2 10", "Main 4:4:4" };

        #endregion

        public VideoEncoderStrategyViewModel()
        {
            UpdateEncoderList();
            UpdatePresetOptions();
            UpdateTuneOptions();
            UpdateProfileOptions();
            UpdateLevelOptions();
            UpdatePixelFormats();

            LocalizationService.LanguageChanged += OnLanguageChanged;
            
            // 发布初始值到OperationContext
            PublishInitialValues();
        }

        private void PublishInitialValues()
        {
            PublishData(nameof(SelectedCodecIndex), SelectedCodecIndex);
            PublishData(nameof(SelectedEncoderIndex), SelectedEncoderIndex);
            PublishData(nameof(PresetValue), PresetValue);
            PublishData(nameof(SelectedTuneIndex), SelectedTuneIndex);
            PublishData(nameof(ProfileValue), ProfileValue);
            PublishData(nameof(SelectedPixelFormatIndex), SelectedPixelFormatIndex);
            PublishData(nameof(LevelValue), LevelValue);
            PublishData(nameof(SelectedRateModeIndex), SelectedRateModeIndex);
            PublishData(nameof(CrfValue), CrfValue);
            PublishData(nameof(TargetBitrate), TargetBitrate);
            PublishData(nameof(QpI), QpI);
            PublishData(nameof(QpP), QpP);
            PublishData(nameof(QpB), QpB);
        }

        private void OnLanguageChanged(object? sender, string e)
        {
            UpdatePresetOptions();
            UpdateTuneOptions();
            UpdateLevelOptions();
            UpdatePixelFormats();
            OnPropertyChanged(nameof(CurrentPresetLabel));
            OnPropertyChanged(nameof(CurrentProfileLabel));
            OnPropertyChanged(nameof(CurrentLevelLabel));
        }

        private void UpdateVisibility()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] UpdateVisibility called");
            System.Diagnostics.Debug.WriteLine($"  ShowEncoderSelector: {ShowEncoderSelector}");
            System.Diagnostics.Debug.WriteLine($"  ShowPresetSlider: {ShowPresetSlider}");
            System.Diagnostics.Debug.WriteLine($"  ShowTuneSelector: {ShowTuneSelector}");
            System.Diagnostics.Debug.WriteLine($"  ShowProfileSlider: {ShowProfileSlider}");
            System.Diagnostics.Debug.WriteLine($"  ShowPixelFormat: {ShowPixelFormat}");
            System.Diagnostics.Debug.WriteLine($"  ShowLevelSlider: {ShowLevelSlider}");
            System.Diagnostics.Debug.WriteLine($"  ShowRateControl: {ShowRateControl}");
#endif
            OnPropertyChanged(nameof(ShowEncoderSelector));
            OnPropertyChanged(nameof(ShowPresetSlider));
            OnPropertyChanged(nameof(ShowTuneSelector));
            OnPropertyChanged(nameof(ShowProfileSlider));
            OnPropertyChanged(nameof(ShowPixelFormat));
            OnPropertyChanged(nameof(ShowLevelSlider));
            OnPropertyChanged(nameof(ShowRateControl));
        }

        private void UpdateEncoderList()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] UpdateEncoderList called, CodecIndex: {SelectedCodecIndex}");
#endif
            AvailableEncoders.Clear();
            switch (SelectedCodecIndex)
            {
                case 1: // H.264
                    AvailableEncoders.Add("x264");
                    AvailableEncoders.Add("NVENC H.264");
                    AvailableEncoders.Add("QSV H.264");
                    AvailableEncoders.Add("AMF H.264");
                    AvailableEncoders.Add("VideoToolbox H.264");
                    break;
                case 2: // H.265
                    AvailableEncoders.Add("x265");
                    AvailableEncoders.Add("NVENC HEVC");
                    AvailableEncoders.Add("QSV HEVC");
                    AvailableEncoders.Add("AMF HEVC");
                    AvailableEncoders.Add("VideoToolbox HEVC");
                    break;
                case 3: // H.266
                    AvailableEncoders.Add("vvenc");
                    break;
                case 4: // AV1
                    AvailableEncoders.Add("libaom-av1");
                    AvailableEncoders.Add("libsvtav1");
                    AvailableEncoders.Add("librav1e");
                    AvailableEncoders.Add("NVENC AV1");
                    AvailableEncoders.Add("QSV AV1");
                    AvailableEncoders.Add("AMF AV1");
                    break;
                case 5: // VP9
                    AvailableEncoders.Add("libvpx-vp9");
                    break;
                case 6: // ProRes
                    AvailableEncoders.Add("prores_ks");
                    AvailableEncoders.Add("prores_aw");
                    break;
                case 7: // 无压缩
                    AvailableEncoders.Add("rawvideo");
                    AvailableEncoders.Add("ffv1");
                    AvailableEncoders.Add("huffyuv");
                    break;
            }
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] AvailableEncoders count: {AvailableEncoders.Count}");
#endif
            
            // 确保集合更新完成后再设置索引
            if (AvailableEncoders.Count > 0)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[VideoEncoder] Setting SelectedEncoderIndex to 0 (current: {_selectedEncoderIndex})");
#endif
                // 关键修复：如果当前已经是0，先设置为-1再设置为0，确保触发属性变更
                if (_selectedEncoderIndex == 0)
                {
                    SelectedEncoderIndex = -1;
                }
                SelectedEncoderIndex = 0;
            }
        }

        /// <summary>
        /// 更新所有依赖于编码器选择的选项
        /// 这个方法确保在编码器名称可用后才执行更新
        /// </summary>
        private void UpdateEncoderDependentOptions()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] UpdateEncoderDependentOptions called");
#endif
            // 确保编码器名称可用
            var encoderName = GetCurrentEncoderName();
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] Current encoder name: '{encoderName}'");
#endif
            if (string.IsNullOrEmpty(encoderName))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[VideoEncoder] Encoder name is empty, scheduling delayed update");
#endif
                // 如果编码器名称还不可用，延迟执行
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => UpdateEncoderDependentOptions()),
                    System.Windows.Threading.DispatcherPriority.DataBind);
                return;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] Updating dependent options for encoder: {encoderName}");
#endif
            // 按顺序更新所有依赖项
            UpdatePresetOptions();
            UpdateTuneOptions();
            UpdateProfileOptions();
            UpdateLevelOptions();
            UpdatePixelFormats();
            UpdateDefaultCrf();
            
            // 确保所有可见性属性都被通知
            UpdateVisibility();
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] UpdateEncoderDependentOptions completed");
#endif
        }

        private void UpdatePresetOptions()
        {
            var encoder = GetCurrentEncoderName();
            if (encoder.Contains("x264") || encoder.Contains("x265"))
            {
                PresetMaxValue = 9;
                PresetMinLabel = LocalizationService.GetString("VideoEncoder_Preset_Min_X264");
                PresetMaxLabel = LocalizationService.GetString("VideoEncoder_Preset_Max_X264");
                PresetValue = 5; // Medium
            }
            else if (encoder.Contains("NVENC"))
            {
                PresetMaxValue = 6;
                PresetMinLabel = LocalizationService.GetString("VideoEncoder_Preset_Min_NVENC");
                PresetMaxLabel = LocalizationService.GetString("VideoEncoder_Preset_Max_NVENC");
                PresetValue = 3; // P4
            }
            else if (encoder.Contains("QSV"))
            {
                PresetMaxValue = 6;
                PresetMinLabel = LocalizationService.GetString("VideoEncoder_Preset_Min_QSV");
                PresetMaxLabel = LocalizationService.GetString("VideoEncoder_Preset_Max_QSV");
                PresetValue = 3; // Medium
            }
            else if (encoder.Contains("libaom") || encoder.Contains("libsvtav1") || encoder.Contains("librav1e"))
            {
                PresetMaxValue = 12;
                PresetMinLabel = LocalizationService.GetString("VideoEncoder_Preset_Min_AV1");
                PresetMaxLabel = LocalizationService.GetString("VideoEncoder_Preset_Max_AV1");
                PresetValue = 6;
            }
            else
            {
                PresetMaxValue = 9;
                PresetMinLabel = LocalizationService.GetString("VideoEncoder_Preset_Fast");
                PresetMaxLabel = LocalizationService.GetString("VideoEncoder_Preset_Slow");
                PresetValue = 5;
            }
            OnPropertyChanged(nameof(CurrentPresetLabel));
        }

        private void UpdateTuneOptions()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] UpdateTuneOptions called");
#endif
            AvailableTunes.Clear();
            var isX264OrX265 = IsX264OrX265Encoder();
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] IsX264OrX265Encoder: {isX264OrX265}");
#endif
            if (isX264OrX265)
            {
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_None"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_Film"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_Animation"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_Grain"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_Stillimage"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_Fastdecode"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_Zerolatency"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_PSNR"));
                AvailableTunes.Add(LocalizationService.GetString("VideoEncoder_Tune_SSIM"));
                SelectedTuneIndex = 0;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[VideoEncoder] Added {AvailableTunes.Count} tune options");
#endif
            }
            else
            {
                SelectedTuneIndex = -1; // 清空选择
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[VideoEncoder] Not x264/x265, cleared tune options");
#endif
            }
            // 关键：必须通知ShowTuneSelector属性变化
            OnPropertyChanged(nameof(ShowTuneSelector));
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] ShowTuneSelector notified, value: {ShowTuneSelector}");
#endif
        }

        private void UpdateProfileOptions()
        {
            if (SelectedCodecIndex == 1) // H.264
            {
                ProfileMaxValue = 2;
            }
            else if (SelectedCodecIndex == 2) // H.265
            {
                ProfileMaxValue = 4;
            }
            // H.266 (codecIndex == 3) 不支持profile参数，不设置
            ProfileValue = 0;
            OnPropertyChanged(nameof(CurrentProfileLabel));
            OnPropertyChanged(nameof(ShowProfileSlider));
        }

        private void UpdateLevelOptions()
        {
            if (SelectedCodecIndex == 1) // H.264
            {
                LevelMaxValue = GetH264Levels().Length - 1;
                LevelMaxLabel = "6.2";
            }
            else if (SelectedCodecIndex == 2 || SelectedCodecIndex == 3) // H.265/H.266
            {
                LevelMaxValue = GetH265Levels().Length - 1;
                LevelMaxLabel = "6.2";
            }
            LevelValue = 0;
            OnPropertyChanged(nameof(CurrentLevelLabel));
            OnPropertyChanged(nameof(ShowLevelSlider));
        }

        private void UpdatePixelFormats()
        {
            AvailablePixelFormats.Clear();
            AvailablePixelFormats.Add(LocalizationService.GetString("VideoEncoder_Auto"));
            AvailablePixelFormats.Add("YUV420P");
            AvailablePixelFormats.Add("YUV420P10LE");
            AvailablePixelFormats.Add("YUV422P");
            AvailablePixelFormats.Add("YUV422P10LE");
            AvailablePixelFormats.Add("YUV444P");
            AvailablePixelFormats.Add("YUV444P10LE");
            AvailablePixelFormats.Add("NV12");
            AvailablePixelFormats.Add("P010LE");
            SelectedPixelFormatIndex = 0;
        }

        private void UpdateDefaultCrf()
        {
            var encoder = GetCurrentEncoderName();
            if (encoder.Contains("x264"))
                DefaultCrfValue = 23;
            else if (encoder.Contains("x265"))
                DefaultCrfValue = 28;
            else if (encoder.Contains("libaom") || encoder.Contains("libsvtav1"))
                DefaultCrfValue = 30;
            else
                DefaultCrfValue = 23;
        }

        private string GetCurrentEncoderName()
        {
            if (SelectedEncoderIndex >= 0 && SelectedEncoderIndex < AvailableEncoders.Count)
                return AvailableEncoders[SelectedEncoderIndex];
            return "";
        }

        private bool IsX264OrX265Encoder()
        {
            var encoder = GetCurrentEncoderName();
            var result = encoder.Contains("x264") || encoder.Contains("x265");
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[VideoEncoder] IsX264OrX265Encoder: encoder='{encoder}', result={result}");
#endif
            return result;
        }

        private string GetCurrentPresetLabel()
        {
            var encoder = GetCurrentEncoderName();
            if (encoder.Contains("x264") || encoder.Contains("x265"))
            {
                var presets = GetX264X265Presets();
                if (PresetValue >= 0 && PresetValue < presets.Length)
                    return presets[PresetValue];
            }
            else if (encoder.Contains("NVENC"))
            {
                var presets = GetNvencPresets();
                if (PresetValue >= 0 && PresetValue < presets.Length)
                    return presets[PresetValue];
            }
            else if (encoder.Contains("QSV"))
            {
                var presets = GetQsvPresets();
                if (PresetValue >= 0 && PresetValue < presets.Length)
                    return presets[PresetValue];
            }
            else if (encoder.Contains("libaom") || encoder.Contains("libsvtav1") || encoder.Contains("librav1e"))
            {
                var presets = GetAv1Presets();
                if (PresetValue >= 0 && PresetValue < presets.Length)
                    return presets[PresetValue];
            }
            return PresetValue.ToString();
        }

        private string GetCurrentProfileLabel()
        {
            if (SelectedCodecIndex == 1)
            {
                var profiles = H264Profiles;
                if (ProfileValue >= 0 && ProfileValue < profiles.Length)
                    return profiles[ProfileValue];
            }
            if (SelectedCodecIndex == 2)
            {
                var profiles = H265Profiles;
                if (ProfileValue >= 0 && ProfileValue < profiles.Length)
                    return profiles[ProfileValue];
            }
            return ProfileValue.ToString();
        }

        private string GetCurrentLevelLabel()
        {
            if (LevelValue == 0)
                return LocalizationService.GetString("VideoEncoder_Auto");
            if (SelectedCodecIndex == 1)
            {
                var levels = GetH264Levels();
                if (LevelValue >= 0 && LevelValue < levels.Length)
                    return levels[LevelValue];
            }
            if (SelectedCodecIndex == 2 || SelectedCodecIndex == 3)
            {
                var levels = GetH265Levels();
                if (LevelValue >= 0 && LevelValue < levels.Length)
                    return levels[LevelValue];
            }
            return LevelValue.ToString();
        }

        private string[] GetX264X265Presets() => new[]
        {
            LocalizationService.GetString("VideoEncoder_Preset_ultrafast"),
            LocalizationService.GetString("VideoEncoder_Preset_superfast"),
            LocalizationService.GetString("VideoEncoder_Preset_veryfast"),
            LocalizationService.GetString("VideoEncoder_Preset_faster"),
            LocalizationService.GetString("VideoEncoder_Preset_fast"),
            LocalizationService.GetString("VideoEncoder_Preset_medium"),
            LocalizationService.GetString("VideoEncoder_Preset_slow"),
            LocalizationService.GetString("VideoEncoder_Preset_slower"),
            LocalizationService.GetString("VideoEncoder_Preset_veryslow"),
            LocalizationService.GetString("VideoEncoder_Preset_placebo")
        };

        private string[] GetNvencPresets() => new[]
        {
            "P1 (" + LocalizationService.GetString("VideoEncoder_Preset_Fast") + ")",
            "P2", "P3",
            "P4 (" + LocalizationService.GetString("VideoEncoder_Preset_medium") + ")",
            "P5", "P6",
            "P7 (" + LocalizationService.GetString("VideoEncoder_Preset_Slow") + ")"
        };

        private string[] GetQsvPresets() => new[]
        {
            LocalizationService.GetString("VideoEncoder_Preset_veryfast"),
            LocalizationService.GetString("VideoEncoder_Preset_faster"),
            LocalizationService.GetString("VideoEncoder_Preset_fast"),
            LocalizationService.GetString("VideoEncoder_Preset_medium"),
            LocalizationService.GetString("VideoEncoder_Preset_slow"),
            LocalizationService.GetString("VideoEncoder_Preset_slower"),
            LocalizationService.GetString("VideoEncoder_Preset_veryslow")
        };

        private string[] GetAv1Presets() => new[]
        {
            "0 (" + LocalizationService.GetString("VideoEncoder_Preset_Slow") + ")",
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11",
            "12 (" + LocalizationService.GetString("VideoEncoder_Preset_Fast") + ")"
        };

        private string[] GetH264Levels() => new[]
        {
            LocalizationService.GetString("VideoEncoder_Auto"),
            "1.0", "1.1", "1.2", "1.3", "2.0", "2.1", "2.2",
            "3.0", "3.1", "3.2", "4.0", "4.1", "4.2", "5.0", "5.1", "5.2", "6.0", "6.1", "6.2"
        };

        private string[] GetH265Levels() => new[]
        {
            LocalizationService.GetString("VideoEncoder_Auto"),
            "1.0", "2.0", "2.1", "3.0", "3.1", "4.0", "4.1",
            "5.0", "5.1", "5.2", "6.0", "6.1", "6.2"
        };

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["selectedCodecIndex"] = SelectedCodecIndex,
                ["selectedEncoderIndex"] = SelectedEncoderIndex,
                ["presetValue"] = PresetValue,
                ["selectedTuneIndex"] = SelectedTuneIndex,
                ["profileValue"] = ProfileValue,
                ["selectedPixelFormatIndex"] = SelectedPixelFormatIndex,
                ["levelValue"] = LevelValue,
                ["selectedRateModeIndex"] = SelectedRateModeIndex,
                ["crfValue"] = CrfValue,
                ["targetBitrate"] = TargetBitrate,
                ["qpI"] = QpI,
                ["qpP"] = QpP,
                ["qpB"] = QpB
            };
        }

        public override void Deserialize(JsonObject data)
        {
            if (data.TryGetPropertyValue("selectedCodecIndex", out var sci)) SelectedCodecIndex = sci?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("selectedEncoderIndex", out var sei)) SelectedEncoderIndex = sei?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("presetValue", out var pv)) PresetValue = pv?.GetValue<int>() ?? 5;
            if (data.TryGetPropertyValue("selectedTuneIndex", out var sti)) SelectedTuneIndex = sti?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("profileValue", out var prv)) ProfileValue = prv?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("selectedPixelFormatIndex", out var spfi)) SelectedPixelFormatIndex = spfi?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("levelValue", out var lv)) LevelValue = lv?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("selectedRateModeIndex", out var srmi)) SelectedRateModeIndex = srmi?.GetValue<int>() ?? 0;
            if (data.TryGetPropertyValue("crfValue", out var cv)) CrfValue = cv?.GetValue<int>() ?? 23;
            if (data.TryGetPropertyValue("targetBitrate", out var tb)) TargetBitrate = tb?.GetValue<int>() ?? 5000;
            if (data.TryGetPropertyValue("qpI", out var qi)) QpI = qi?.GetValue<int>() ?? 20;
            if (data.TryGetPropertyValue("qpP", out var qp)) QpP = qp?.GetValue<int>() ?? 23;
            if (data.TryGetPropertyValue("qpB", out var qb)) QpB = qb?.GetValue<int>() ?? 26;
        }
    }
}
