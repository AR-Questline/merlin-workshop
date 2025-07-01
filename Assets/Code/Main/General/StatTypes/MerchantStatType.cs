using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Shops;

namespace Awaken.TG.Main.General.StatTypes {
    public class MerchantStatType : StatType<IMerchant> {

        protected MerchantStatType(string id, string displayName, Func<IMerchant, Stat> getter, string inspectorCategory = "", Param param = null)
            : base(id, displayName, getter, inspectorCategory, param) {
        }
    }
}