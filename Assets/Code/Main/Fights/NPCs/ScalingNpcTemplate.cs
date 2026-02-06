using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class ScalingNpcTemplate : NpcTemplate {
        [FoldoutGroup("Stats"), SerializeField]
        AnimationCurve maxHealthScaling = AnimationCurve.Linear(1, 100, 50, 5000);
        [FoldoutGroup("Stats"), SerializeField]
        AnimationCurve maxStaminaScaling = AnimationCurve.Linear(1, 50, 50, 200);
        [FoldoutGroup("Combat Stats"), SerializeField]
        AnimationCurve meleeDamageScaling = AnimationCurve.Linear(1, 10, 50, 100);
        [FoldoutGroup("Combat Stats"), SerializeField]
        AnimationCurve rangedDamageScaling = AnimationCurve.Linear(1, 10, 50, 100);
        [FoldoutGroup("Combat Stats"), SerializeField]
        AnimationCurve magicDamageScaling = AnimationCurve.Linear(1, 10, 50, 100);
        [FoldoutGroup("Combat Stats"), SerializeField]
        AnimationCurve forceStumbleThresholdScaling = AnimationCurve.Linear(1, 25, 50, 1250);
        [FoldoutGroup("Combat Stats"), SerializeField]
        AnimationCurve poiseThresholdScaling = AnimationCurve.Linear(1, 40, 50, 2000);
        
        public override int MaxHealth => ScaledMaxHealth(1);
        public override int MaxStamina => (int) maxStaminaScaling.Evaluate(1);
        public override float MeleeDamage => meleeDamageScaling.Evaluate(1);
        public override float RangedDamage => rangedDamageScaling.Evaluate(1);
        public override float MagicDamage => magicDamageScaling.Evaluate(1);
        public override float ForceStumbleThreshold => forceStumbleThresholdScaling.Evaluate(1);
        public override float PoiseThreshold => poiseThresholdScaling.Evaluate(1);

        protected override bool HideScalingStats => true;

        public int ScaledMaxHealth(int heroLevel) => (int) maxHealthScaling.Evaluate(heroLevel);
        public int ScaledMaxStamina(int heroLevel) => (int) maxStaminaScaling.Evaluate(heroLevel);
        public float ScaledMeleeDamage(int heroLevel) => meleeDamageScaling.Evaluate(heroLevel);
        public float ScaledRangedDamage(int heroLevel) => rangedDamageScaling.Evaluate(heroLevel);
        public float ScaledMagicDamage(int heroLevel) => magicDamageScaling.Evaluate(heroLevel);
        public float ScaledForceStumbleThreshold(int heroLevel) => forceStumbleThresholdScaling.Evaluate(heroLevel);
        public float ScaledPoiseThreshold(int heroLevel) => poiseThresholdScaling.Evaluate(heroLevel);

#if UNITY_EDITOR
        [FoldoutGroup("Stats"), ShowInInspector, HideLabel]
        string HpScaling => GetScaledStatValueAtMarkerLevels("hp", maxHealthScaling);
        [FoldoutGroup("Stats"), ShowInInspector, HideLabel]
        string StaminaScaling => GetScaledStatValueAtMarkerLevels("sp", maxStaminaScaling);
        [FoldoutGroup("Combat Stats"), ShowInInspector, HideLabel]
        string MeleeDmgScaling => GetScaledStatValueAtMarkerLevels("melee", meleeDamageScaling);
        [FoldoutGroup("Combat Stats"), ShowInInspector, HideLabel]
        string RangedDmgScaling => GetScaledStatValueAtMarkerLevels("ranged", rangedDamageScaling);
        [FoldoutGroup("Combat Stats"), ShowInInspector, HideLabel]
        string MagicDmgScaling => GetScaledStatValueAtMarkerLevels("magic", magicDamageScaling);
        [FoldoutGroup("Combat Stats"), ShowInInspector, HideLabel]
        string StumbleScaling => GetScaledStatValueAtMarkerLevels("stumble", forceStumbleThresholdScaling);
        [FoldoutGroup("Combat Stats"), ShowInInspector, HideLabel]
        string PoiseScaling => GetScaledStatValueAtMarkerLevels("poise", poiseThresholdScaling);
        
        string GetScaledStatValueAtMarkerLevels(string name, AnimationCurve curve) {
            int[] levels = new[] { 1, 25, 50, 75, 100 };
            var sb = new StringBuilder();
            sb.Append($"{name} Scaling: ");
            foreach (var level in levels) {
                sb.Append($"[ L{level} - {curve.Evaluate(level):f2} ] ");
            }
            return sb.ToString();
        }
#endif
    }
}