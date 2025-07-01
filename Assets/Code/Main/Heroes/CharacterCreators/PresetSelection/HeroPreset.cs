using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection {
    [SpawnsView(typeof(VHeroPreset))]
    public partial class HeroPreset : Element<PresetSelector> {
        public readonly CharacterBuildPreset Preset;
        
        public HeroPreset(CharacterBuildPreset preset) {
            Preset = preset;
        }
        
        public void SelectPreset() {
            ParentModel.SelectPreset(Preset);
        }
    }
}
