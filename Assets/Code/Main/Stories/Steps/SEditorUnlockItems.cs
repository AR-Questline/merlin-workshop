using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Items: Unlock Items")]
    public class SEditorUnlockItems : EditorStep {
        public bool unlockAllItems;
        public bool unlockAllLoadouts;
        public bool unlockAllEquipment;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SUnlockItems {
                unlockAllItems = unlockAllItems,
                unlockAllLoadouts = unlockAllLoadouts,
                unlockAllEquipment = unlockAllEquipment
            };
        }
    }
    
    public partial class SUnlockItems : StoryStep {
        public bool unlockAllItems;
        public bool unlockAllLoadouts;
        public bool unlockAllEquipment;
        
        public override StepResult Execute(Story story) {
            var heroItems = Hero.Current.HeroItems;

            if (unlockAllItems) {
                foreach (var item in heroItems.Items) {
                    foreach (var lockItemSlot in item.Elements<LockItemSlot>()) {
                        if (lockItemSlot.Source == LockItemSlot.LockSource.Story) {
                            lockItemSlot.Discard();
                        }
                    }
                }
            }

            if (unlockAllLoadouts) {
                foreach (var loadout in heroItems.Loadouts) {
                    foreach (var locker in loadout.Elements<HeroLoadoutSlotLocker>().ToArraySlow()) {
                        locker.Discard();
                    }
                }
            }

            if (unlockAllEquipment) {
                heroItems.LockEquipping(false);
            }
            
            return StepResult.Immediate;
        }
    }
}