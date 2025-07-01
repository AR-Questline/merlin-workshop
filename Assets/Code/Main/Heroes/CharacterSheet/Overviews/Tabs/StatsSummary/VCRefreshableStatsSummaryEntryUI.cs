using System;
using System.Globalization;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.StatsSummary {
    public class VCRefreshableStatsSummaryEntryUI : VCStatsSummaryEntryUI {
        [Title("Dependent stat type")]
        [SerializeField, RichEnumExtends(typeof(StatType))] 
        RichEnumReference[] dependentStats = Array.Empty<RichEnumReference>();
        
        public StatType[] DependentStats { get; private set; } = Array.Empty<StatType>();
        Func<float, float, float> PredictedValueGetter => OwnPredictedValueGetter ?? DefaultPredictedValueGetter;
        Func<float, float, float> DefaultPredictedValueGetter => (_, changeValue) => changeValue;
        Func<float, float, float> OwnPredictedValueGetter { get; set; }
        
        float PredictedValue { get; set; }
        float _changeValue;
        
        public void PrepareDependentStats() {
            // DependentStats = dependentStats + MaxStat (if withMaxStat)
            if (dependentStats.Length > 0 || withMaxStat) {;
                int extra = withMaxStat ? 1 : 0;
                DependentStats = new StatType[dependentStats.Length + extra];
                
                for (int index = 0; index < dependentStats.Length; index++) {
                    RichEnumReference stat = dependentStats[index];
                    DependentStats[index] = stat.EnumAs<StatType>();
                }
                
                if (withMaxStat) {
                    DependentStats[^1] = MaxStatType;
                }
            }
        }

        public void Setup(float changeValue) {
            _changeValue = PredictedValueGetter(PredictedValue, changeValue);
        }
        
        public override void Refresh() {
            base.Refresh();
            PredictedValue = ValueGetter();
        }
        
        public void Override(Func<float> valueGetter, Func<float, float, float> predicateValueGetter = null, string name = null) {
            OwnPredictedValueGetter = predicateValueGetter;
            Override(valueGetter, name);
            Refresh();
        }
        
        public void PredictApplyValue(int count) {
            PredictedValue += _changeValue * count;
            DisplayPredictedValue();
        }
        
        void DisplayPredictedValue() {
            bool predictedValueChange = !Mathf.Approximately(PredictedValue, ValueGetter());
            string predictedValue = PredictedValue.ToString(valueFormat, CultureInfo.InvariantCulture).ColoredText(predictedValueChange ? ARColor.MainAccent : ARColor.MainGrey);
            UpdateStatValueLabel(predictedValue);
        }
    }
}