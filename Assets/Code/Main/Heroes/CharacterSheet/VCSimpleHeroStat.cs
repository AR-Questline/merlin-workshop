using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.Utility.Maths;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    /// <summary>
    /// Used to display a simple hero stat in the character sheet footer: HP, SP, MP
    /// </summary>
    public class VCSimpleHeroStat : ViewComponent {
        [RichEnumExtends(typeof(StatType))] 
        [SerializeField] RichEnumReference stat;
        [RichEnumExtends(typeof(StatType))] 
        [SerializeField] RichEnumReference subStat;

        [SerializeField, LocStringCategory(Category.UI)] LocString simpleStatName;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI valueText;
        
        [SerializeField] string defaultLimitedFormat = "{0}/{1}";
        [SerializeField] string underLimitFormat = "<color={0}><style=\"Semi-Bold\">{1}</style></color>/{2}";
        [SerializeField] Color underLimitHighlight;

        StatType StatType => stat.EnumAs<StatType>();
        StatType SubStatType => subStat.EnumAs<StatType>();
        
        protected override void OnAttach() {
            Initialize();
            Refresh(Hero.Current.Stat(StatType));
        }

        void Initialize() {
            var hero = Hero.Current;
            nameText.SetText(simpleStatName);
            hero.ListenTo(Stat.Events.StatChanged(StatType), Refresh, this);
            if (SubStatType != null) {
                hero.ListenTo(Stat.Events.StatChanged(SubStatType), () => Refresh(hero.Stat(StatType)), this);
            }
        }

        void Refresh(Stat stat) {
            if (stat is LimitedStat limitedStat) {
                valueText.text = limitedStat.ModifiedInt < limitedStat.UpperLimitInt
                    ? string.Format(underLimitFormat, underLimitHighlight.ToHex(), limitedStat.ModifiedInt, limitedStat.UpperLimitInt)
                    : string.Format(defaultLimitedFormat, limitedStat.ModifiedInt, limitedStat.UpperLimitInt);
            } else {
                valueText.text = stat.ModifiedInt.ToString();
            }
        }
    }
}