using Awaken.TG.Main.Heroes.CharacterCreators.Parts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    [UsesPrefab("CharacterCreator/VCharacterCreatorName")]
    public class VCharacterCreatorName : VCharacterCreatorTab {
        [SerializeField] VCCPartHost heroName;
        [SerializeField] VGenericPromptUI acceptPromptView;
        
        Prompt _acceptPrompt;
        ARInputField _nameInputField;
        
        public override HeroRenderer.Target ViewTarget => HeroRenderer.Target.CCBody;

        protected override void OnFullyInitialized() {
            Add(new CCHeroName(), heroName);
            _acceptPrompt = CharacterCreator.Prompts
                .BindPrompt(Prompt.Tap(KeyBindings.UI.Settings.ApplyChanges, LocTerms.Confirm.Translate(), CharacterCreator.SaveAndClose), Target, acceptPromptView)
                .AddAudio()
                .SetupState(true, false);
            
            _nameInputField = World.Only<CCHeroName>().View<VCCHeroName>().InputField;
            _nameInputField.OnSelected += _ => RefreshAcceptPrompt();
            
            ReceiveFocus();
        }
        
        void RefreshAcceptPrompt() {
            bool active = !_nameInputField.IsSelected && !string.IsNullOrEmpty(_nameInputField.TMPInputField.text);
            _acceptPrompt.SetActive(active);
        }
    }
}