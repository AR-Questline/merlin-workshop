using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    public class VCCraftingTabButton : CraftingTabs.VCHeaderTabButton {
        [RichEnumExtends(typeof(CraftingTabTypes))] 
        [SerializeField] RichEnumReference tabType;
        public override CraftingTabTypes Type => tabType.EnumAs<CraftingTabTypes>();
        public override string ButtonName => Type.Title;
    }
}
