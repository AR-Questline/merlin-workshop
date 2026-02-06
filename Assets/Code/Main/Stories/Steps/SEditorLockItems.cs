using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Items: Lock Items")]
    public class SEditorLockItems : EditorStep {
        public bool lockAllItems;
        public bool lockAllLoadouts;
        public bool lockAllEquipment;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLockItems {
                lockAllItems = lockAllItems,
                lockAllLoadouts = lockAllLoadouts,
                lockAllEquipment = lockAllEquipment
            };
        }
    }
    
    public partial class SLockItems : StoryStep {
        public bool lockAllItems;
        public bool lockAllLoadouts;
        public bool lockAllEquipment;
        
        public override StepResult Execute(Story story) {
            var heroItems = Hero.Current.HeroItems;

            if (lockAllItems) {
                foreach (var item in heroItems.Items) {
                    bool found = false;
                    foreach (var lockItemSlot in item.Elements<LockItemSlot>()) {
                        if (lockItemSlot.Source == LockItemSlot.LockSource.Story) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        item.AddElement(new LockItemSlot(true, LockItemSlot.LockSource.Story));
                    }
                }    
            }

            if (lockAllLoadouts) {
                foreach (var loadout in heroItems.Loadouts) {
                    loadout.AddElement(new HeroLoadoutSlotLocker(EquipmentSlotType.MainHand));
                    loadout.AddElement(new HeroLoadoutSlotLocker(EquipmentSlotType.OffHand));
                    loadout.AddElement(new HeroLoadoutSlotLocker(EquipmentSlotType.Quiver));
                }
            }

            if (lockAllEquipment) {
                heroItems.LockEquipping(true);
            }
            
            return StepResult.Immediate;
        }
    }
}