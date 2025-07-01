using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class VCCharacterStatGreaterThan : ViewComponent {
        [RichEnumExtends(typeof(CharacterStatType))]
        public RichEnumReference statType;

        [SerializeField] TextMeshProUGUI msgInfoText;
        [SerializeField, LocStringCategory(Category.UI)] LocString msgToDisplay;
        [SerializeField] int statValue;
        [SerializeField] GameObject parent;

        Hero Hero => Hero.Current;
        IWithStats WithStats => Hero;
        StatType StatType => statType.EnumAs<CharacterStatType>();

        protected override void OnAttach() {
            WithStats.ListenTo(Stat.Events.StatChanged(StatType), _ => UpdateStat(), this);
            UpdateStat();

            if (msgInfoText != null) {
                msgInfoText.SetText(msgToDisplay);
            }
        }
        
        void UpdateStat() {
            msgInfoText.gameObject.SetActive(Hero.Stat(StatType).ModifiedInt > statValue);
        }
    }
}