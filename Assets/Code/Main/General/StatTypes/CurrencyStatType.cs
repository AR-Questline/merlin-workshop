using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("Shops")]
    public class CurrencyStatType : MerchantStatType {

        public static readonly CurrencyStatType
            Wealth = new CurrencyStatType(nameof(Wealth), LocTerms.Wealth, m => m.Wealth, "", new Param{Abbreviation = "$", Tooltip = LocTerms.WealthDescription}),
            Cobweb = new CurrencyStatType(nameof(Cobweb), LocTerms.Cobweb, m => m.Cobweb, "", new Param{Abbreviation = "C", Tooltip = LocTerms.CobwebDescription});

        CurrencyStatType(string id, string displayName, Func<IMerchant, Stat> getter,
                         string inspectorCategory = "", Param param = null)
            : base(id, displayName, getter, inspectorCategory, param) { }
    }
}