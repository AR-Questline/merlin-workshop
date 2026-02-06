using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public class TheTowerOfBoneAndTimberCoordinatorAttachment : MonoBehaviour, IAttachmentSpec {
        [BoxGroup("Stage 1"), SerializeField, InlineProperty, HideLabel]
        StageSettings stage1 = new() { activeTime = 10f, inactiveTime = 15f, spawnerIntensityMultiplier = 1f };

        [BoxGroup("Stage 2"), SerializeField, InlineProperty, HideLabel]
        StageSettings stage2 = new() { activeTime = 10f, inactiveTime = 10f, spawnerIntensityMultiplier = 1.5f };

        [BoxGroup("Stage 3"), SerializeField, InlineProperty, HideLabel]
        StageSettings stage3 = new() { activeTime = 10f, inactiveTime = 5f, spawnerIntensityMultiplier = 2f };

        [BoxGroup("Stage 4"), SerializeField, InlineProperty, HideLabel]
        StageSettings stage4 = new() { activeTime = 10f, inactiveTime = 5f, spawnerIntensityMultiplier = 2f };
        
        [Title("End of Fight")]
        public StoryBookmark endOfFightStory;
        public GameObject endOfFightToDisable;
        
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        public EndOfFightMovement[] endOfFightMovements = Array.Empty<EndOfFightMovement>();

        [InlineButton(nameof(SetSpawnerToCameraPosition)), Space]
        public Vector3 spawnerPosition;
        public float rangeFromSpawnerPointToMoveGeysers = 20f;
        public float rangeFromSpawnerPointToPlaceSpawner = 1;
        public float geyserRepositionInterval = 20f;
        
        [Space, Title("Shout")]
        [InlineButton(nameof(SetShoutToCameraPosition))]
        public Vector3 shoutPosition;
        
        [SerializeField, InlineProperty, HideLabel, Indent]
        public SphereDamageSerializableParameters shoutSkill = new() {
            vfxDuration = 3f,
            damageRadius = 30f,
            damageDuration = 0.5f,
            damageAmount = 50f,
            poiseDamage = 20f,
            forceDamage = 30f,
            ragdollForce = 1000f,
            hitMask = -1,
            damageType = DamageType.Environment,
            damageSubType = DamageSubType.GenericPhysical,
            inevitable = true,
            isPrimary = true,
            ignoreArmor = true,
        };
        
        public string itemDisablingShoutDamageGUID;
        [Space, Title("Geyser Tags")]
        public LocationReference geyserTags;
        [Title("Launcher Tags")]
        public LocationReference launcherTags;
        [Title("Portal to invoke when save is loaded")]
        public LocationReference portalTags;

        public StageSettings GetStageSettings(int stage) {
            return stage switch {
                1 => stage1,
                2 => stage2,
                3 => stage3,
                4 => stage4,
                _ => default
            };
        }

        public Element SpawnElement() => new TheTowerOfBoneAndTimberCoordinator();

        public bool IsMine(Element element) => element is TheTowerOfBoneAndTimberCoordinator;

        void SetSpawnerToCameraPosition() => spawnerPosition = SetPositionFromCamera();
        void SetShoutToCameraPosition() => shoutPosition = SetPositionFromCamera();

        void Awake() {
            if (endOfFightToDisable) {
                endOfFightToDisable.SetActive(true);
            }
            for (int i = 0; i < endOfFightMovements.Length; i++) {
                if (endOfFightMovements[i].transform) {
                    endOfFightMovements[i].transform.gameObject.SetUnityRepresentation(new IWithUnityRepresentation.Options(){linkedLifetime = true, movable = true});
                }
            }
        }

        Vector3 SetPositionFromCamera() {
#if UNITY_EDITOR
            var cameraTransform = UnityEditor.SceneView.lastActiveSceneView.camera.transform;
            return cameraTransform.position + cameraTransform.forward * 3f;
#else
            return Vector3.zero;
#endif
        }

        void OnDrawGizmosSelected() {
            // Spawner position and range
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(spawnerPosition, 3f);
            Gizmos.DrawWireSphere(spawnerPosition, rangeFromSpawnerPointToMoveGeysers);
            
            // Shout position and damage radius
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(shoutPosition, 2f);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(shoutPosition, shoutSkill.damageRadius);
        }
        
        [Serializable]
        public struct EndOfFightMovement {
            public Transform transform;
            [Indent]
            [ShowIf(nameof(transform))]
            public Vector3 targetPosition;
            [Indent]
            [ShowIf(nameof(transform))]
            public float movementDuration;
            public EventReference movementSFX;
        }
        
        [Serializable]
        public struct StageSettings {
            [TemplateType(typeof(LocationTemplate))]
            [SerializeField] TemplateReference spawner;
            public float activeTime;
            public float inactiveTime;
            [Min(0.1f)]
            public float spawnerIntensityMultiplier;
            
            public LocationTemplate Spawner => spawner.Get<LocationTemplate>();
        }
    }
}