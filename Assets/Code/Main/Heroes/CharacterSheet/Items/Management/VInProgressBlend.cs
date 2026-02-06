using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Management {
    [UsesPrefab("Items/" + nameof(VInProgressBlend))]
    public class VInProgressBlend : View<IModel> {
        [SerializeField] TMP_Text inProgressText;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            inProgressText.SetText($"{LocTerms.WorkInProgress.Translate()} ...");
        }
        
        public void Show() {
            gameObject.SetActiveOptimized(true);
        }

        public void Hide() {
            gameObject.SetActiveOptimized(false);
        }
    }
}