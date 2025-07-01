using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class VCAliveStat : StatComponent {
        [Space(10F)]
        [RichEnumExtends(typeof(AliveStatType))]
        public RichEnumReference statType;

        protected override IWithStats WithStats => Hero.Current;
        protected override StatType StatType => statType.EnumAs<AliveStatType>();
    }
}