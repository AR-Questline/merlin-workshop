using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    [UsesPrefab("UI/Tutorials/" + nameof(VTutorialText))]
    public class VTutorialText : VTutorialText<TutorialText> { }

    public class VTutorialText<T> : View<T> where T : TutorialText {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] VGenericPromptUI closeButton;
        [SerializeField] GameObject content;

        [SerializeField] Image background;
        [SerializeField] Color bgColorInInventory;
        [SerializeField] Color bgColorInGameplay;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnTutorials();
        
        protected override void OnInitialize() {
            content.SetActive(false);
            titleText.SetActiveAndText(!string.IsNullOrEmpty(Target.TitleText), Target.TitleText);
            contentText.SetActiveAndText(!string.IsNullOrEmpty(Target.ContentText), Target.ContentText);
            
            var bgColor = Target.Context switch {
                TutorialText.ViewContext.Gameplay => bgColorInGameplay,
                TutorialText.ViewContext.Inventory => bgColorInInventory,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            background.color = bgColor;
        }
        
        protected virtual void ShowContent() {
            InitPrompts();
        }

        public void Show(bool state) {
            content.SetActive(state);
            
            if (state) {
                Services.Get<CanvasService>().ShowTutorialCanvasOnly(Target.DisableOtherCanvases);
                ShowContent();
            }
        }
        
        void InitPrompts() {
            var prompts = Target.AddElement(new Prompts(null));
            prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Target.Close), Target, closeButton);
        }
    }
}