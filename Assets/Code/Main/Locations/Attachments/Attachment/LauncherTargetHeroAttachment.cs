using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [RequireComponent(typeof(InteractionTriggerSkillsAttachment))]
    public class LauncherTargetHeroAttachment : MonoBehaviour, IAttachmentSpec {
        [Serializable]
        public struct DifficultyParameters {
            [RichEnumExtends(typeof(Difficulty)), LabelText("Player Difficulty")]
            public RichEnumReference rawDifficulty;
            [InfoBox("Must be the same value as set in BallistaArrow to work correctly")]
            public float projectileVelocity;
            public float targetRandomRadius;
            [Tooltip("Maximum angle deviation from target to allow firing (in degrees)")]
            public float firingAccuracyAngle;
            public float launchInterval;
            [Tooltip("Maximum rotation speed in degrees per second")]
            public float maxRotationSpeed;
        }

        [Title("Activation parameters")]
        public bool activeOnlyWhenLocationInCombat;
        [ShowIf(nameof(activeOnlyWhenLocationInCombat))] 
        public LocationReference locationThatHasToBeInCombat;
        
        [Title("Difficulty dependent parameters")]
        [SerializeField]
        List<DifficultyParameters> difficultyParameters;
        
        [Title("Ballista Tracking limits")]
        public float minDistanceToTrack = 10f;
        public float maxDistanceToAttack = 100f;
        public string ballistaHeadTag;
        public float pitchLimit = 45f;
        public float yawLimit = 90f;
        
        [Title("Firing parameters")]
        public float randomInitialDelayDecrease = 5f;
        public float randomIntervalDelta = 4f;

        [Title("Operator configs")]
        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference operatorLocation;
        [ShowIf(nameof(RequireOperatorToFunction))]
        public bool spawnOperatorAtStart = true;
        [Indent, ShowIf(nameof(RequireOperatorToFunction))]
        public float operatorRespawnDelay = -1f;
        [ShowIf(nameof(RequireOperatorToFunction))]
        public string operatorSeatTag;
        [ShowIf(nameof(RequireOperatorToFunction))]
        public bool returnToBaseRotationWithoutOperator;
        
        
        public bool RequireOperatorToFunction => operatorLocation.IsSet;
        public LocationTemplate OperatorLocation => operatorLocation.TryGet<LocationTemplate>();
        
        public DifficultyParameters GetParametersForDifficulty(Difficulty difficulty) {
            if (difficultyParameters == null || difficultyParameters.Count == 0) {
                return new DifficultyParameters {
                    projectileVelocity = 100f,
                    targetRandomRadius = 2f,
                    firingAccuracyAngle = 5f,
                    launchInterval = 30f,
                    maxRotationSpeed = 30f
                };
            }

            DifficultyParameters defaultSettings = default;
            // Find exact match
            for (int i = 0; i < difficultyParameters.Count; i++) {
                if (difficultyParameters[i].rawDifficulty == difficulty) {
                    return difficultyParameters[i];
                }
                if (difficultyParameters[i].rawDifficulty == null) {
                    defaultSettings = difficultyParameters[i];
                }
            }
            
            // Fallback to first entry with null difficulty (default)
            if (defaultSettings.rawDifficulty != null) {
                return defaultSettings;
            }
            
            // Last resort: return first entry
            return difficultyParameters[0];
        }
        
        public Element SpawnElement() => new LauncherTargetHero();
        public bool IsMine(Element element) => element is LauncherTargetHero;

        void OnDrawGizmosSelected() {
            var ballistaBase = transform;
            var ballistaHead = string.IsNullOrEmpty(ballistaHeadTag) ? transform : transform.Find(ballistaHeadTag);
            if (ballistaHead == null) {
                ballistaHead = gameObject.FindChildWithTagRecursively(ballistaHeadTag);
            }
            if (ballistaHead == null) {
                ballistaHead = transform;
            }

            var position = ballistaHead.position;
            
            // Draw range spheres
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, minDistanceToTrack);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, maxDistanceToAttack);
            
            // Draw pitch limit arcs
            BallistaPitchLimitGizmos.DrawPitchLimits(position, ballistaBase, pitchLimit);
            
            // Draw yaw limit arcs
            BallistaPitchLimitGizmos.DrawYawLimits(position, ballistaBase, yawLimit);
        }
    }
}