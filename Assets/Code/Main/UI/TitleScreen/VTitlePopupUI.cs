using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/" + nameof(VTitlePopupUI))]
    public class VTitlePopupUI : View<TitlePopupUI>, IAutoFocusBase {
        [SerializeField] VGenericPromptUI cancel;
        [SerializeField] VGenericPromptUI accept;
        [SerializeField] TMP_Text text;
        
        Prompts _prompts;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            text.text = Target.MessageText;
            
            _prompts = Target.AddElement(new Prompts(null));
            _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Confirm, LocTerms.Yes.Translate(), Target.AcceptClicked), Target, accept);
            _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.No.Translate(), Target.CancelClicked), Target, cancel);
        }
    }
}
