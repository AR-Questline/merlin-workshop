using System;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    [Serializable]
    public class StatusStatsValues : ISerializationCallbackReceiver {
        public const float CantGetBuildupValue = 9999f;
        const int DefaultThreshold = 100;
        const float WeakThresholdMultiplier = 0.5f;
        const float ResistantThresholdMultiplier = 2f;
        
        [InfoBox("DisplayType modifies what Stats are visible in inspector. All Stats are existing in code, even if not visible.")]
        [SerializeField, OnValueChanged(nameof(UpdateEditorDrawer))] 
        StatusStatDisplayType displayType = StatusStatDisplayType.NPC;
        [InfoBox("Threshold: How much buildup you need to apply to fill the buildup bar.\nEffect Str: how strong the effect is. For custom use (how much damage it deals etc.) or duration while active tweak (how long am I frozen).")]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, ShowPaging = false), ShowInInspector, NonSerialized] 
        StatusStatValue[] _filteredValuesToDisplay;
        
        [SerializeField, TemplateType(typeof(StatusTemplate))] public TemplateReference[] invulnerableToStatuses = Array.Empty<TemplateReference>();
        
        [HideInInspector, SerializeField] StatusStatValue _bleed = new (BuildupStatusType.Bleed);
        [HideInInspector, SerializeField] StatusStatValue _burn = new (BuildupStatusType.Burn);
        [HideInInspector, SerializeField] StatusStatValue _frenzy = new (BuildupStatusType.Frenzy);
        [HideInInspector, SerializeField] StatusStatValue _confusion = new (BuildupStatusType.Confusion);
        [HideInInspector, SerializeField] StatusStatValue _corruption = new (BuildupStatusType.Corruption);
        [HideInInspector, SerializeField] StatusStatValue _mute = new (BuildupStatusType.Mute);
        [HideInInspector, SerializeField] StatusStatValue _poison = new (BuildupStatusType.Poison);
        [HideInInspector, SerializeField] StatusStatValue _slow = new (BuildupStatusType.Slow);
        [HideInInspector, SerializeField] StatusStatValue _stun = new (BuildupStatusType.Stun);
        [HideInInspector, SerializeField] StatusStatValue _weak = new (BuildupStatusType.Weak);
        [HideInInspector, SerializeField] StatusStatValue _drunk = new (BuildupStatusType.Drunk);
        [HideInInspector, SerializeField] StatusStatValue _intoxicated = new (BuildupStatusType.Intoxicated);
        [HideInInspector, SerializeField] StatusStatValue _full = new (BuildupStatusType.Full);
        
        public StatusStatValue Bleed => _bleed;
        public StatusStatValue Burn => _burn;
        public StatusStatValue Frenzy => _frenzy;
        public StatusStatValue Confusion => _confusion;
        public StatusStatValue Corruption => _corruption;
        public StatusStatValue Mute => _mute;
        public StatusStatValue Poison => _poison;
        public StatusStatValue Slow => _slow;
        public StatusStatValue Stun => _stun;
        public StatusStatValue Weak => _weak;
        public StatusStatValue Drunk => _drunk;
        public StatusStatValue Intoxicated => _intoxicated;
        public StatusStatValue Full => _full;

        public TemplateReference[] InvulnerableToStatuses => invulnerableToStatuses;
        
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
            UpdateEditorDrawer();
        }

        void UpdateEditorDrawer() {
            _filteredValuesToDisplay = GatherFilteredValues();
        }
        
        StatusStatValue[] GatherFilteredValues() {
            switch (displayType) {
                case StatusStatDisplayType.NPC:
                    return new[] { _burn, _confusion, _frenzy, _poison, _slow };
                case StatusStatDisplayType.Hero:
                case StatusStatDisplayType.ShowAll:
                default:
                    return new[] { _bleed, _burn, _confusion, _corruption, _mute, _frenzy, _poison, _slow, _stun, _weak, _drunk, _intoxicated, _full };
            }
        }

        [Serializable, HideReferenceObjectPicker]
        public class StatusStatValue {
            [SerializeField] StatusBuildupThreshold buildupThreshold;
            [SerializeField] StatusEffectModifier effectStrength;
            [SerializeField] [UnityEngine.Scripting.Preserve] string buildupName;
            
            public StatusStatValue(BuildupStatusType buildupStatusBuildupType) {
                buildupThreshold = StatusBuildupThreshold.Normal;
                effectStrength = StatusEffectModifier.Normal;
                buildupName = buildupStatusBuildupType.EnumName;
            }

            public float GetThreshold(int tier) => StatusStatsValues.GetThreshold(buildupThreshold, tier);
            public float GetModifier() => StatusStatsValues.GetModifier(effectStrength);
        }
        
        public static float GetThreshold(StatusBuildupThreshold buildupThresholdThreshold, int tier) {
            float threshold = tier <= 0 ? DefaultThreshold : DefaultThreshold + (tier - 1) * (tier * 25);
            return buildupThresholdThreshold switch {
                StatusBuildupThreshold.Weak => threshold * WeakThresholdMultiplier,
                StatusBuildupThreshold.Normal => threshold,
                StatusBuildupThreshold.Resistant => threshold * ResistantThresholdMultiplier,
                StatusBuildupThreshold.CantGet => CantGetBuildupValue,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static float GetModifier(StatusEffectModifier effectStrength) {
            return effectStrength switch {
                StatusEffectModifier.Vulnerable => 2f,
                StatusEffectModifier.Normal => 1f,
                StatusEffectModifier.Immune => 0.5f,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public enum StatusBuildupThreshold : byte {
            Weak = 0,
            Normal = 1,
            Resistant = 2,
            CantGet = 3
        }

        public enum StatusEffectModifier : byte {
            Vulnerable = 0,
            Normal = 1,
            Immune = 2
        }
        
        internal enum StatusStatDisplayType : byte {
            NPC,
            Hero,
            ShowAll,
        }
    }
}