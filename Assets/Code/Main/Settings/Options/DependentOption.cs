using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Options {
    public class DependentOption : PrefOption {
        readonly Func<bool> _showDependentAlternativeEvaluation;
        public sealed override Type ViewType => typeof(VDependentOption);

        public override bool WasChanged => AllOptions.Any(o => o.WasChanged); 

        public PrefOption BaseOption { get; }
        public PrefOption[] OtherOptions { get; }
        public bool ShowDependent => _showDependentAlternativeEvaluation?.Invoke() ?? BaseOption is not ToggleOption option || option.Enabled;
        IEnumerable<PrefOption> AllOptions => BaseOption.Yield().Concat(OtherOptions);
        
        public DependentOption(PrefOption baseOption, params PrefOption[] otherOptions) : base("", "", false) {
            BaseOption = baseOption;
            OtherOptions = otherOptions;
        }
        
        public DependentOption(PrefOption baseOption, Func<bool> showDependentAlternativeEvaluation, params PrefOption[] otherOptions) : base("", "", false) {
            BaseOption = baseOption;
            OtherOptions = otherOptions;
            _showDependentAlternativeEvaluation = showDependentAlternativeEvaluation;
        }

        public override void ForceChange() {
            foreach (var option in AllOptions) {
                option.ForceChange();
            }
        }

        public override void Apply() {
            foreach (var option in AllOptions) {
                option.Apply();
            }
        }

        public override void Cancel() {
            foreach (var option in AllOptions) {
                option.Cancel();
            }
        }

        public override void RestoreDefault() {
            foreach (var option in AllOptions) {
                option.RestoreDefault();
            }
        }
    }
}