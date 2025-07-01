using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class VCHeroCurrency : StatComponent {
        [Space(10F)]
        [RichEnumExtends(typeof(CurrencyStatType))]
        public RichEnumReference statType;

        protected override IWithStats WithStats => Hero.Current;
        protected override StatType StatType => statType.EnumAs<CurrencyStatType>();
    }
}