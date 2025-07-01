using System;
using System.Globalization;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.StatsSummary {
    public class VCStatsSummaryEntryUI : ViewComponent {
        [Title("Stat type")]
        [SerializeField, FormerlySerializedAs("stat"), RichEnumExtends(typeof(StatType))] 
        RichEnumReference mainStat;
        [SerializeField] 
        protected bool withMaxStat;
        [SerializeField, RichEnumExtends(typeof(StatType)), Tooltip("Additional stat to display value / max value (e.g. health / max health)")] 
        [CanBeNull, ShowIf(nameof(withMaxStat))]
        RichEnumReference maxStat;
        
        [Title("UI controls")]
        [SerializeField] TextMeshProUGUI statName;
        [SerializeField] TextMeshProUGUI statValue;
        [SerializeField] bool isPerSecond;
        [SerializeField, ShowIf(nameof(isPerSecond))] GameObject perSecondText;
        
        [Title("Format strings")]
        [SerializeField] string maxValueFormat = "{0}/{1}";
        [SerializeField] protected string valueFormat = "P0";

        public StatType MainStatType => mainStat.EnumAs<StatType>();
        Stat MainStat => Hero.Current.Stat(MainStatType);
        public StatType MaxStatType => maxStat?.EnumAs<StatType>();
        Stat MaxStat => Hero.Current.Stat(MaxStatType);
        
        protected Func<float> ValueGetter => OwnValueGetter ?? DefaultValueGetter;
        Func<float> DefaultValueGetter => () => withMaxStat ? MaxStat.ModifiedValue : MainStat.ModifiedValue;
        Func<float> OwnValueGetter { get; set; }
        
        protected override void OnAttach() {
            statName.text = MainStatType == null ? statName.text : MainStatType.DisplayName;
            perSecondText.SetActive(isPerSecond);
        }
        
        public void Override(Func<float> valueGetter, string name = null) {
            OwnValueGetter = valueGetter;
            
            if (string.IsNullOrEmpty(name) == false) {
                statName.text = name;
            }
        }
        
        public void Override(string name, string value) {
            statName.text = name;
            statValue.text = value;
        }

        public virtual void Refresh() {
            UpdateStatValueLabel(ValueGetter().ToString(valueFormat, CultureInfo.InvariantCulture));
        }

        protected void UpdateStatValueLabel(string valueString) {
            statValue.text = withMaxStat ? string.Format(maxValueFormat, MainStat.ModifiedValue.ToString(valueFormat, CultureInfo.InvariantCulture), valueString) : valueString;
        }
    }
}