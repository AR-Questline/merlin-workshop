using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [Serializable]
    public struct MonsterEggTarget {
        public Vector3 position;
        [TemplateType(typeof(LocationTemplate))]
        public TemplateReference spawnerToSpawn;
    }

    [Serializable]
    public struct ExplosionConfig {
        public bool enabled;
        [ShowIf(nameof(enabled))]
        public float radius;
        [ShowIf(nameof(enabled))]
        public float duration;
        [ShowIf(nameof(enabled))]
        public float damage;
        [ShowIf(nameof(enabled))]
        public DamageType damageType;
        [ShowIf(nameof(enabled))]
        public float forceDamage;
        [ShowIf(nameof(enabled))]  
        public float poiseDamage;
        [ShowIf(nameof(enabled))]
        [TemplateType(typeof(LocationTemplate))]
        public TemplateReference persistentAoE;

        public static ExplosionConfig Default => new() {
            enabled = false,
            radius = 5f,
            duration = 0.5f,
            damage = 50f,
            damageType = DamageType.MagicalHitSource,
            forceDamage = 0,
            poiseDamage = 0
        };
    }

    /// <summary>
    /// Has a custom editor MonsterEggLauncherAttachmentEditor
    /// </summary>
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Launches monster egg projectiles to target locations that trigger spawners")]
    public class MonsterEggLauncherAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField]
        public bool shouldStartEnabled = true;
        [PrefabAssetReference]
        public ShareableARAssetReference projectileAsset;
        public float projectileSpeed = 15f;
        [Tooltip("Use high arc trajectory for projectiles")]
        public bool highShot = true;
        [Header("Launch Timing")]
        public float launchIntervalMin = 10f;
        public float launchIntervalMax = 20f;
        [Header("Target Settings")]
        public float eggLandingOffset = 3f;
        [Tooltip("Maximum distance from hero to target position to allow launching")]
        public float maxDistanceOfTargetFromHero = 50f;
        [Tooltip("Whether to predict hero movement when selecting targets")]
        public bool shouldUsePrediction;
        public ExplosionConfig explosion = ExplosionConfig.Default;
        public MonsterEggTarget[] targets = Array.Empty<MonsterEggTarget>();

        
        public Element SpawnElement() => new MonsterEggLauncher();
        public bool IsMine(Element element) => element is MonsterEggLauncher;
        
#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            if (targets == null) return;

            // Draw connections from launcher to targets
            Gizmos.color = Color.yellow;
            var launcherPos = transform.position;
            
            for (int i = 0; i < targets.Length; i++) {
                var worldPos = transform.TransformPoint(targets[i].position);
                Gizmos.DrawLine(launcherPos, worldPos);
                
                // Draw target position sphere
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(worldPos, 0.5f);
                
                // Draw filled sphere for better visibility
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawSphere(worldPos, 0.5f);
                
                // Reset color for next line
                Gizmos.color = Color.yellow;
            }
            
            // Draw launcher position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(launcherPos, 0.3f);
        }
#endif
    }
}