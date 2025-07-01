using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.PatchNotes {
    [UsesPrefab("TitleScreen/VPatchNotesUI")]
    public class VPatchNotesUI : View<PatchNotesUI> {
        public ARButton closeButton;
        public TextMeshProUGUI title;
        public TextMeshProUGUI message;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            closeButton.OnClick += Target.Discard;
            Refresh();
        }

        void Refresh() {
            message.text = Target.Note.message;
            title.text = Target.Note.title;
        }
    }
}