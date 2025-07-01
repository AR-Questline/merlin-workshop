using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Sessions;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI {
    [CreateAssetMenu(fileName = "Perception", menuName = "NpcData/Perception")]
    public class PerceptionData : ScriptableObject {
        const float OutsideOpenWorldMultiplier = 0.666f;
        static float RangeMultiplier => (World.Services.TryGet<SceneService>()?.IsOpenWorld ?? true) ? 1 : OutsideOpenWorldMultiplier;

        [FoldoutGroup("View Range"), SerializeField, Tooltip("Core distance hero can be seen"), OnValueChanged(nameof(RecalculateAngles))] 
        float coreDistance;
        [FoldoutGroup("View Range"), SerializeField, Tooltip("Max distance hero can be seen"), OnValueChanged(nameof(RecalculateAngles))] 
        float maxDistance;
        [FoldoutGroup("View Range"), SerializeField, Tooltip("Core distance hero can be seen when in combat")] 
        float inCombatCoreDistance;
        [FoldoutGroup("View Range"), SerializeField, Tooltip("Max distance hero can be seen when in combat")] 
        float inCombatMaxDistance;
        [FoldoutGroup("Angles"), SerializeField, Tooltip("Core angle hero can be seen"), OnValueChanged(nameof(RecalculateAngles))] 
        float coreAngle;
        [FoldoutGroup("Angles"), SerializeField, Tooltip("Max angle hero can be seen"), OnValueChanged(nameof(RecalculateAngles))] 
        float maxAngle;
        [FoldoutGroup("Angles"), SerializeField, Tooltip("Core angle hero can be seen, when in combat"), OnValueChanged(nameof(RecalculateAngles))] 
        float inCombatCoreAngle;
        [FoldoutGroup("Angles"), SerializeField, Tooltip("Max angle hero can be seen, when in combat"), OnValueChanged(nameof(RecalculateAngles))] 
        float inCombatMaxAngle;
        [FoldoutGroup("Angles"), SerializeField]
        float visionCutoff = 0.15f;
        
        [FoldoutGroup("Ranges"), SerializeField, Tooltip("Radius that AI can detect hostile NPCs in")] float radarRange;
        [FoldoutGroup("Ranges"), SerializeField, Tooltip("Radius in which AI will instantly see Hero if is alerted")] float heroRadarRange = 5f;

        [FoldoutGroup("Ranges"), SerializeField, Tooltip("Range that AI will try to inform other AIs about it hits in combat")] float combatHitsInformRange = 12f;
        [FoldoutGroup("Ranges"), SerializeField, Tooltip("Max range AI can call other AIs provided they can see it")] float maxInformRange = 25f;
        [FoldoutGroup("Ranges"), SerializeField, Tooltip("Core range AI can call other AIs event if they cannot see it")] float coreInformRange = 0.3f;
        
        [FoldoutGroup("Ranges"), SerializeField, Tooltip("Max range AI can hear sounds provided they can see it (HeroFootSteps or Combat)")] float maxHearingRange = 10f;
        [FoldoutGroup("Ranges"), SerializeField, Tooltip("Core range AI can hear sounds even if they cannot see ir (HeroFootSteps or Combat)")] float coreHearingRange = 5f;

        [SerializeField]
        bool ignoreAngleFactor;
        [SerializeField, HideIf(nameof(ignoreAngleFactor))]
        bool useCustomFactors;
        [SerializeField, ShowIf(nameof(useCustomFactors))] float customAngleFactor, customCombatAngleFactor;
        
        [SerializeField, ReadOnly] float angleFactor, combatAngleFactor;
        [SerializeField, ReadOnly] float maxDot, inCombatMaxDot;
        
        Cached<PerceptionData, float2> _viewConeSinCosCombat = new(static data => ViewConeSinCosCombat(data));
        Cached<PerceptionData, float2> _viewConeSinCosIdle = new(static data => ViewConeSinCosIdle(data));
        
        public float MaxDistance(NpcAI ai) => ai.InCombat? inCombatMaxDistance : maxDistance;
        public float MaxDistanceSqr(NpcAI ai) => MaxDistance(ai) * MaxDistance(ai);
        public float CoreDistance(NpcAI ai) => ai.InCombat ? inCombatCoreDistance : coreDistance;
        [UnityEngine.Scripting.Preserve] public float CoreAngle(NpcAI ai) => ai.InCombat ? inCombatCoreAngle : coreAngle;
        public float MaxAngle(NpcAI ai) => ai.InCombat ? inCombatMaxAngle : maxAngle;
        public float MaxDot(NpcAI ai) => ai.InCombat ? inCombatMaxDot : maxDot;
        public float AngleFactor(NpcAI ai) => ai.InCombat
            ? combatAngleFactor
            : angleFactor;
        public bool IgnoreAngleFactor => ignoreAngleFactor;
        public float VisionCutoff => visionCutoff;
        public void ViewConeSinCos(NpcAI ai, out float sin, out float cos) {
            var sincos = ai.InCombat ? _viewConeSinCosCombat.Get(this) : _viewConeSinCosIdle.Get(this);
            sin = sincos.x;
            cos = sincos.y;
        }

        public float HeroRadarRange => heroRadarRange;
        public float HeroRadarRangeSq => math.square(HeroRadarRange);
        public float RadarRange => radarRange * RangeMultiplier;
        public float RadarRangeSq => math.square(RadarRange);
        public float CombatHitsInformRange => combatHitsInformRange * RangeMultiplier;
        public float MaxInformRange => maxInformRange * RangeMultiplier;
        public float CoreInformRange => coreInformRange * RangeMultiplier;
        public float MaxHearingRange => maxHearingRange * RangeMultiplier;
        public float CoreHearingRange => coreHearingRange * RangeMultiplier;

        [Button]
        void RecalculateAngles() {
            maxDot = math.cos(maxAngle * Mathf.Deg2Rad);
            inCombatMaxDot = math.cos(inCombatMaxAngle * Mathf.Deg2Rad);
            
            if (useCustomFactors) {
                angleFactor = customAngleFactor;
                combatAngleFactor = customCombatAngleFactor;
            } else if (maxDistance - coreDistance == 0) {
                angleFactor = 1;
                combatAngleFactor = 1;
            } else {
                angleFactor = CalculatedAngleFactor(maxDot, coreDistance, maxDistance);
                combatAngleFactor = CalculatedAngleFactor(inCombatMaxDot, inCombatCoreDistance, inCombatMaxDistance);
            }
        }

        [ShowInInspector]
        public bool RefreshDebug { get; set; }
        
        /// <summary>
        /// Angle Factor is a magic number that is needed to create a smooth vision blend that ends behind an enemy.
        /// It's needed to allow sneaking directly behind a target.
        /// It is calculated by inverting perception equation with RefDistanceForAngle and dot of max percieved angle
        /// When you use this coefficient it results in AI side vision being exactly 0 at point (distance: RefDistanceForAngle, angleDot: dot).
        /// Rest of vision space spreads linearly (in polar coords) leaving blank spot just behind AI
        /// </summary>
        float CalculatedAngleFactor(float dot, float coreDist, float maxDist) {
            // That's the distance at MaxDot at which the result should be 0
            const float RefDistanceForAngle = 5f;
            float distanceF = (RefDistanceForAngle - coreDist) / (maxDist - coreDist);
            return (distanceF - dot) / (1 - distanceF);
        }

        static float2 ViewConeSinCosCombat(PerceptionData data) {
            var angle = data.inCombatMaxAngle * Mathf.Deg2Rad;
            float2 result;
            math.sincos(angle, out result.x, out result.y);
            return result;
        }

        static float2 ViewConeSinCosIdle(PerceptionData data) {
            var angle = data.maxAngle * Mathf.Deg2Rad;
            float2 result;
            math.sincos(angle, out result.x, out result.y);
            return result;
        }
    }
}