using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCProficiencyBar : VCStatBar<Model> {
        [SerializeField] TMP_Text statName;
        [SerializeField, RichEnumExtends(typeof(ProfStatType))]
        RichEnumReference profRef;

        int _previousValue;

        ProfStatType Prof => profRef.EnumAs<ProfStatType>();
        Hero Hero => Hero.Current;

        protected override IWithStats TargetWithStats => Hero;
        protected override StatType StatType => Prof;
        protected override float Percentage => Hero.ProficiencyStats.GetProgressToNextLevel(Prof);
        protected override bool ShouldHide => false;
        
        protected override void OnAttach() {
            base.OnAttach();
            statName.SetText(Prof.DisplayName);
        }

        protected override void SetBarPercentage() {
            if (StatValue > _previousValue) {
                Bar.SetPercentInstant(Percentage);
            } else {
                Bar.SetPercent(Percentage);
            }
            _previousValue = StatValue;
        }
    }
}