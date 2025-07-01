using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.ActiveEffects;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency;
using Awaken.TG.Main.UI.RawImageRendering;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo {
    public partial class CharacterInfoUI : CharacterSubTab<VCharacterInfoUI> {
        public CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;

        protected override void AfterViewSpawned(VCharacterInfoUI view) {
            InitializeProficienciesUI();
            CharacterSheetUI.SetRendererTargetInstant(HeroRenderer.Target.HeroUIStatus);
            CharacterSheetUI.SetHeroOnRenderVisible(true);
        }

        void InitializeProficienciesUI() {
            AddElement(new ActiveEffectsUI());
            AddElement(new ProficienciesUI());
        }
    }
}