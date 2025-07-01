using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickTool : VCQuickItemBase {
        [Space(10f)]
        [SerializeField, RichEnumExtends(typeof(ToolType))] RichEnumReference toolType;
        
        ToolType ToolType => toolType.EnumAs<ToolType>();

        protected override Item RetrieveItem() {
            return HeroItems.Items
                .OrderByDescending(item => item.Quality.Priority)
                .FirstOrDefault(item => item.TryGetElement<Tool>()?.Type == ToolType);
        }

        public override void UseItemAction() {
            if (_item is not { HasBeenDiscarded: false }) {
                return;
            }
            
            if (!Hero.Current.HasMovementType(MovementType.Glider)) {
                HeroItems.LoadoutAt(HeroLoadout.HiddenLoadoutIndex).EquipItem(null, _item);
                HeroItems.ActivateLoadout(HeroLoadout.HiddenLoadoutIndex, false);
            }

            RadialMenu.Close();
        }
    }
}