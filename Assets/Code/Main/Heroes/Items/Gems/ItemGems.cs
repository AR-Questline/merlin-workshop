using Awaken.TG.Main.Skills;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.Gems {
    public partial class ItemGems : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.ItemGems;

        [Saved] public int AvailableSlots { get; private set; }
        [Saved] public int MaxSlots { get; private set; }

        public bool CanAttach => FreeSlots > 0;
        public int FreeSlots => AvailableSlots - (int)ParentModel.Elements<GemAttached>().Count();
        public bool CanIncreaseSlots => AvailableSlots < MaxSlots;

        Item Item => ParentModel;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        ItemGems() {}
        
        public ItemGems(int availableSlots, int maxSlots) {
            AvailableSlots = availableSlots;
            MaxSlots = maxSlots;
        }
        
        public GemAttached AttachGem(Item gemItem) {
            if (!CanAttach) return null;

            GemUnattached gem = gemItem.Element<GemUnattached>();
            if (gem.GemType == GemType.Weapon && !Item.IsWeapon) return null;
            if (gem.GemType == GemType.Armor && !Item.IsArmor) return null;
            
            GemAttached attached = new(gem);
            foreach (var skillRef in gem.SkillRefs) {
                Skill s = skillRef.CreateSkill();
                attached.AddElement(s);
            }
            
            Item.AddElement(attached);
            gemItem.DecrementQuantity();
            return attached;
        }

        public void IncreaseLimit() {
            ChangeLimit(AvailableSlots + 1);
        }

        public void ChangeLimit(int count) {
            if (!CanIncreaseSlots) {
                return;
            }
            
            AvailableSlots = count;
            TriggerChange();
        }
    }
}