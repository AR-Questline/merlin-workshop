using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    [UsesPrefab("Crafting/" + nameof(VCraftingTabsUI))]
    public class VCraftingTabsUI : VTabParent<CraftingTabsUI> {
        [field: SerializeField] public Transform PromptHost { get; private set; }

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        protected override bool CanNestInside(View view) => false;
    }
}
