using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Actions {
    public sealed partial class SarrasSickleInteractAction : LootInteractAction {
        public override ushort TypeForSerialization => SavedModels.SarrasSickleInteractAction;
        
        public override InfoFrame ActionFrame {
            get {
                bool suitable = false;
                bool canBeUsed = false;
                foreach (var item in HeroItems.Items)
                {
                    IsToolSuitable(item, out bool itemSuitable, out bool itemCanBeUsed);
                    if (!itemSuitable) continue;
                    suitable = true;
                    canBeUsed = itemCanBeUsed;
                    if (canBeUsed) {
                        break;
                    }
                }

                if (suitable && canBeUsed) {
                    return new InfoFrame(_requiredToolType.InteractionName, true);
                }
                
                return suitable
                    ? new InfoFrame(LocTerms.SarrasSickleNotCharged.Translate(), false)
                    : new InfoFrame(LocTerms.Blocked.Translate(), false);
            }
        }

        void IsToolSuitable(Item item, out bool suitable, out bool canBeUsed) {
            suitable = false;
            canBeUsed = false;
            if (item?.TryGetElement(out Tool tool) ?? false) {
                suitable = tool.Type == _requiredToolType;
                canBeUsed = tool.CanBeUsed;
            }
        }
    }
}