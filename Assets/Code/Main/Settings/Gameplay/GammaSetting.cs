using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.GammaSettingScreen;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class GammaSetting : Setting {
        const string PrefKey = "GammaSetting";

        // === Options
        float _value;
        
        public sealed override string SettingName => LocTerms.SettingsGamma.Translate();

        public override IEnumerable<PrefOption> Options => Option.Yield();
        public float Value {
            get => _value;
            set {
                _value = value; 
                PrefMemory.Set(PrefKey, value, false);
                this.Trigger(Events.SettingRefresh, this);
            }
        }

        ButtonOption Option { get; }

        // === Initialization
        public GammaSetting() {
            _value = PrefMemory.GetFloat(PrefKey, 1f);
            Option = new ButtonOption(PrefKey, SettingName, static () => GammaScreen.ShowGammaScreen().Forget());
        }
    }
}
