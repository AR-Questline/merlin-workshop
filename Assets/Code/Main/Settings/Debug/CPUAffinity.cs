#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Threads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.Main.Settings.Debug {
    public partial class CPUAffinity : Setting {
        const string PrefId = "CPUAffinity";
        const string DisplayFormat = "{0}";

        public SliderOption Option { get; }
        bool _wasActivated;

        public override IEnumerable<PrefOption> Options => Option.Yield();
        public override string SettingName => LocTerms.SettingsCPUAffinity.Translate();
        protected override bool AutoApplyOnInit => false;
        
        static string SettingTooltip => LocTerms.SettingsCPUAffinityTooltip.Translate();

        public CPUAffinity() {
            Option = new(PrefId, SettingName, 3, Environment.ProcessorCount, true, DisplayFormat, Environment.ProcessorCount, false);
            Option.AddTooltip(() => SettingTooltip);
            if (Option.Value != Environment.ProcessorCount) {
                SetCpuAffinity(Option.Value);
            }
        }

        protected override void OnApply() {
            SetCpuAffinity(Option.Value);
        }

        void SetCpuAffinity(float value) {
            _wasActivated = true;
            ProcessAffinity.TryGetCpuMask(value.ToString(), Enumerable.Range(0, (int)value).ToArray(), out var mask, out _);
            ProcessAffinity.Setup(0, mask, out string result);
            Log.Important?.Warning(result);
        }

        public override bool RequiresRestart => _wasActivated && Option.Value == Environment.ProcessorCount;
    }
}
#endif