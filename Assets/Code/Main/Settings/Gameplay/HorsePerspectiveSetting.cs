using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public class HorsePerspectiveSetting : Setting {
        EnumArrowsOption EnumOption { get; }
        public sealed override string SettingName => LocTerms.SettingsHorsePerspective.Translate();
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();
        
        protected override bool AutoApplyOnInit => false;

        readonly ToggleOption[] _toggleOptions = {
            new ("current", LocTerms.SettingsCurrentPlayerPerspective.Translate(), false, true),
            new ("tpp", LocTerms.SettingsPerspectiveTPP.Translate(), false, true),
            new ("fpp", LocTerms.SettingsPerspectiveFPP.Translate(), false, true),
        };

        public bool IsTPP {
            get => EnumOption.Option == _toggleOptions[1] || (EnumOption.Option == _toggleOptions[0] && Hero.TppActive);
            set {
                if (EnumOption.Option == _toggleOptions[0]) {
                    var perspectiveSetting = World.Any<PerspectiveSetting>();
                    if (perspectiveSetting) {
                        perspectiveSetting.IsTPP = value;
                    }
                    return;
                }
                
                var desiredOption = value ? _toggleOptions[1] : _toggleOptions[2];
                if (desiredOption != EnumOption.Option) {
                    EnumOption.Option = desiredOption;
                    EnumOption.Apply();
                }
            }
        }

        public HorsePerspectiveSetting() {
            EnumOption = new EnumArrowsOption("HorsePerspective_Setting", SettingName, _toggleOptions[2], true, _toggleOptions);
            EnumOption.SetInteractabilityFunction(static () => {
                var vHeroController = Hero.Current?.VHeroController;
                return vHeroController == null || vHeroController.CanChangeHeroPerspective;
            });
        }

        protected override void OnApply() {
            if (Hero.Current != null && Hero.Current.MovementSystem is MountedMovement { HasBeenDiscarded: false }) {
                MountedMovement.SetHeroPerspective(IsTPP).Forget();
            }
        }
    }
}