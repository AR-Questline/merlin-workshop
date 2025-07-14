using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems.Upgrades {
    [UsesPrefab("Gems/Upgrades/" + nameof(VUpgradesUI))]
    public class VUpgradesUI : View<UpgradesUI>, IAutoFocusBase, IFocusSource {
        [SerializeField] ButtonConfig craftButton,
            upgradeButton,
            addGemButton,
            weightReductionButton,
            unlockEffectButton;
        
        public bool ForceFocus => false;
        public Component DefaultFocus => upgradeButton.button;

        public override Transform DetermineHost() => World.Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            craftButton.InitializeButton(() => UIUtils.AddOverlayUIView(Target.Craft(), this), LocTerms.Handcrafting.Translate());
            upgradeButton.InitializeButton(() => UIUtils.AddOverlayUIView(Target.Upgrade(), this), LocTerms.SharpenTab.Translate());
            addGemButton.InitializeButton(() => UIUtils.AddOverlayUIView(Target.AddGem(), this), LocTerms.GemAttachingTab.Translate());
            weightReductionButton.InitializeButton(() => UIUtils.AddOverlayUIView(Target.WeightReduction(), this), LocTerms.ArmorWeightReductionTab.Translate());
        }
    }
}