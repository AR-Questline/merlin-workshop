using System;
using Awaken.TG.Editor.BalanceTool.Presets;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Properties;

namespace Awaken.TG.Editor.BalanceTool.Data {
    public class StatEntry {
        public string name;
        public string description;
        public string notes;
            
        public string id;
        public float lastModifiers;
        public string addPerLevelStat;
            
        public StatType type;
        public HeroRPGStatType modifiedBy;

        public float effective;
        public string effectiveFormula = $"Formula:\n<i>{nameof(BaseValue)} + {nameof(Modifiers)}</i>";

        float ComputeEffective => _computeEffective?.Invoke(this) ?? DefaultEffectiveFormula();
        Func<StatEntry, float> _computeEffective;

        [DontCreateProperty] 
        float _baseValue;

        [CreateProperty] 
        public float BaseValue
        {
            get => _baseValue;
            set {
                _baseValue = value;
                UpdateEffective();
            }
        }
            
        [DontCreateProperty] 
        float _modifiers;

        [CreateProperty] 
        public float Modifiers
        {
            get => _modifiers;
            set {
                lastModifiers = _modifiers;
                _modifiers = value;
                UpdateEffective();
            }
        }
            
        [DontCreateProperty] 
        float _addPerLevel;

        [CreateProperty] 
        public float AddPerLevel
        {
            get => _addPerLevel;
            set {
                _addPerLevel = value;
                UpdateEffective();
            }
        }
            
        public StatEntry(string name, float baseValue, string description = "", float addPerLevel = 0.0f, string notes = "") {
            this.name = name;
            this.description = description;
            this.notes = notes;

            BaseValue = baseValue;
            AddPerLevel = addPerLevel;
        }

        public StatEntry(StatEntryPreset preset) {
            OverrideStat(preset);
        }
        
        public void OverrideStat(StatType type, HeroRPGStatType modifiedBy, float baseValue) {
            this.type = type;
            this.modifiedBy = modifiedBy;
            BaseValue = baseValue;
            
            GameConstants.Get.RPGStatParamsByType[modifiedBy].Effects.ForEach(effect => {
                if (effect.StatEffected == type && (effect.EffectType == OperationType.Add || effect.EffectType == OperationType.AddPreMultiply)) {
                    AddPerLevel = effect.BaseEffectStrength;
                }
            });
        }

        public void OverrideStat(StatEntryPreset preset) {
            name = preset.stat.name;
            description = preset.stat.description;
                
            BaseValue = preset.stat.baseValue;
            AddPerLevel = preset.stat.addPerLevel;
            Modifiers = preset.stat.modifiers;
        }
            
        public void UpdateEffective() {
            effective = ComputeEffective;
        }
            
        public void SetEffectiveFormula(Func<StatEntry, float> computeEffective) {
            _computeEffective = computeEffective;
        }
        
        public void SetEffectiveFormula(StatEntry statType) {
            addPerLevelStat = statType.name;
            effectiveFormula = $"Formula:\n<i>{nameof(BaseValue)} + {nameof(Modifiers)} + ({nameof(AddPerLevel)} * {addPerLevelStat} levelFactor)</i>";
            _computeEffective = _ => DefaultEffectiveFormula(() => BalanceToolCalculator.ComputeStatLevel(statType));
        }
        
        float DefaultEffectiveFormula() => BaseValue + Modifiers;
        float DefaultEffectiveFormula(Func<float> levelFactorGetter) => BaseValue + Modifiers + AddPerLevel * levelFactorGetter.Invoke();
    }
}