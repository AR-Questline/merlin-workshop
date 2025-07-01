using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class VCTalentPoints : VCPoints {
        [SerializeField, CanBeNull] TMP_Text availablePointsText;
        [SerializeField, RichEnumExtends(typeof(StatType))]
        RichEnumReference statType = CharacterStatType.TalentPoints;
        
        protected override StatType StatType => statType.EnumAs<StatType>();
        
        protected override void OnStatUpdated() {
            base.OnStatUpdated();

            if (availablePointsText != null) {
                availablePointsText.text = LocTerms.AvailablePoints.Translate();
            }
        }
    }
}