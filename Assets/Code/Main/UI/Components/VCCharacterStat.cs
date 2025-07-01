using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class VCCharacterStat : StatComponent {
        [Space(10F), SerializeField] bool heroStats;
        [RichEnumExtends(typeof(CharacterStatType)), HideIf(nameof(heroStats))]
        public RichEnumReference statType;
        [RichEnumExtends(typeof(HeroStatType)), ShowIf(nameof(heroStats))]
        public RichEnumReference heroStatType;

        protected override IWithStats WithStats => Hero.Current;
        protected override StatType StatType => heroStats ? heroStatType.EnumAs<HeroStatType>() : statType.EnumAs<CharacterStatType>();
    }
}