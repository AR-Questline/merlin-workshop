using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.ActiveEffects;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo {
    public partial class CharacterInfoUI : CharacterSubTab<VCharacterInfoUI> {
        public CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;

        protected override void AfterViewSpawned(VCharacterInfoUI view) {
            InitializeProficienciesUI();
            CharacterSheetUI.SetRendererTargetInstant(HeroRenderer.Target.HeroUIStatus);
            CharacterSheetUI.SetHeroOnRenderVisible(true);
            CharacterSheetUI.Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Open.Translate(), Prompt.Position.First, ControlSchemeFlag.Gamepad), this);
        }

        void InitializeProficienciesUI() {
            AddElement(new ActiveEffectsUI());
            AddElement(new ProficienciesUI());
        }
    }
}