using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.Heroes.Statuses {
    public partial class ItemBuffFakeStatus : Status {
        public sealed override bool IsNotSaved => true;

        public ItemBuffFakeStatus(AppliedItemBuff itemBuff, ItemTemplate buffTemplate) : 
            base(GameConstants.Get.DefaultItemBuffStatus, StatusSourceInfo.FromItemBuff(itemBuff, buffTemplate), null) { }
    }
}