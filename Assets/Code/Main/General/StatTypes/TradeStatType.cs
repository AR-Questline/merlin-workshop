using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("Shops")]
    public class TradeStatType : MerchantStatType {

        /// <summary>
        /// It modifies the price when merchant buys item
        /// </summary>
        public static readonly TradeStatType 
            BuyModifier = new TradeStatType(nameof(BuyModifier), LocTerms.BuyModifier, m => m.BuyModifier, "", new Param{Abbreviation = "Buy$"});

        /// <summary>
        /// It modifies the price when merchant sells item
        /// </summary>
        public static readonly TradeStatType 
            SellModifier = new TradeStatType(nameof(SellModifier), LocTerms.SellModifier, m => m.SellModifier, "", new Param{Abbreviation = "Sell$"});


        TradeStatType(string id, string displayName, Func<IMerchant, Stat> getter,
            string inspectorCategory = "", Param param = null)
            : base(id, displayName, getter, inspectorCategory, param) { }
    }

}