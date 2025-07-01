using Awaken.TG.Main.Heroes.CharacterCreators.Parts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    [UsesPrefab("CharacterCreator/VCharacterCreatorBody")]
    public class VCharacterCreatorBody : VCharacterCreatorTab {
        [SerializeField] VCCPartHost gender;
        [SerializeField] VCCPartHost bodyNormal;
        [SerializeField] VCCPartHost skinColor;
        [SerializeField] VGenericPromptUI randomPromptView;
        [SerializeField] ButtonConfig randomButton;

        public override HeroRenderer.Target ViewTarget => HeroRenderer.Target.CCBody;

        protected override void OnFullyInitialized() {
            Add(CCSliderData.Gender, gender);
            Add(CCSliderData.Normals, bodyNormal);
            Add(CCGridSelectData.SkinColor, skinColor);

            randomButton.InitializeButton(CharacterCreator.RandomizePreset);
            var randomPrompt = Prompt.Tap(KeyBindings.UI.Settings.RestoreDefaults, LocTerms.Random.Translate(), CharacterCreator.RandomizePreset);
            CharacterCreator.Prompts.BindPrompt(randomPrompt, Target, randomPromptView).AddAudio();
            ReceiveFocus();
        }
    }
}