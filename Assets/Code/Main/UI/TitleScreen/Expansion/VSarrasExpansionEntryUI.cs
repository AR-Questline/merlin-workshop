using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    [UsesPrefab("TitleScreen/Expansion/" + nameof(VSarrasExpansionEntryUI))]
    public class VSarrasExpansionEntryUI : VExpansionEntryUI {
        [SerializeField] TextMeshProUGUI expansionText;
        [SerializeField] VGenericPromptUI readMorePrompt;

        protected override void OnInitialize() {
            base.OnInitialize();
            
            var prompts = Target.AddElement(new Prompts(null));
            prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Expansion.DlcReadMore, LocTerms.ExpansionReadMore.Translate(), OpenExpansionOverview), Target, readMorePrompt);
        }
    }
}