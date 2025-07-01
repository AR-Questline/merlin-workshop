using Awaken.TG.Main.Localization;
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
            craftButton.InitializeButton(Target.Craft, LocTerms.Handcrafting.Translate());
            upgradeButton.InitializeButton(Target.Upgrade, LocTerms.SharpenTab.Translate());
            addGemButton.InitializeButton(Target.AddGem, LocTerms.GemAttachingTab.Translate());
            weightReductionButton.InitializeButton(Target.WeightReduction, LocTerms.ArmorWeightReductionTab.Translate());
            unlockEffectButton.InitializeButton(Target.UnlockEffect, "Unlock Effect");
        }
    }
}