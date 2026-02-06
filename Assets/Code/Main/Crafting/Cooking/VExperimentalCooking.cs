using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    [UsesPrefab("Crafting/Cooking/" + nameof(VExperimentalCooking))]
    public class VExperimentalCooking : VCrafting<ExperimentalCooking>, IAutoFocusBase {
        [field: SerializeField] public Transform IngredientsGridUI { get; private set; }

        public override Transform DetermineHost() => Target.ParentModel.ContentHost;
        protected override bool IsInteractable => Target.ButtonInteractability || Target.WorkbenchHasItems;
        protected override float HoldTime => 0.25f;

        protected override void OnInitialize() {
            base.OnInitialize();
            StaticTooltip.TrySetActiveOptimized(true);
        }

        protected override void CreateKeyHeld() {
            Target.PossibleResultTooltipUI.DisappearTooltip();
        }
    }
}