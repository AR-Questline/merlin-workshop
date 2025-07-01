using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Spawners {
    [ExecuteInEditMode]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Spawns NPCs, used for simple cases, Group Spawner handles all of them.")]
    public class LocationSpawnerAttachment : SpawnerAttachment, IAttachmentSpec {
        [InfoBox("Cannot be unique npc", InfoMessageType.Error, nameof(NotRepetitiveNpc))]
        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference[] locationsToSpawn = Array.Empty<TemplateReference>();
        [Range(1, 20)] public int spawnAmount;
        [Range(0, 125)] public float spawnerRange;
        [DisableIf("@" + nameof(discardAfterSpawn))]
        public bool overrideSpawnerCooldown;
        [DisableIf("@" + nameof(discardAfterSpawn)), ShowIf(nameof(overrideSpawnerCooldown))]
        [Tooltip("Cooldown in seconds, counted in real time of playing, not influenced by resting.")]
        public float spawnerCooldown = DefaultSpawnerCooldown;
        [Space]
        [LabelText("Spawn Once"), DisableIf(nameof(discardAfterAllKilled)), DisableIf(nameof(discardSpawnedLocationsOnDiscard))]
        public bool discardAfterSpawn;
        [LabelText("Discard On Full Clear"), DisableIf(nameof(discardAfterSpawn))]
        public bool discardAfterAllKilled;
        [DisableIf("@" + nameof(discardAfterSpawn) + " || " + nameof(discardAfterAllKilled))]
        public bool mustFullClearToRespawn;
        [Space]
        public bool snapToGroundOnSpawn;

        public IEnumerable<LocationTemplate> LocationsToSpawn => locationsToSpawn.Where(r => r.IsSet).Select(r => r.Get<LocationTemplate>());
        public float SpawnerCooldown => overrideSpawnerCooldown ? spawnerCooldown : DefaultSpawnerCooldown;

        bool NotRepetitiveNpc => locationsToSpawn.Any(UniqueNpcUtils.IsUnique);
        
        public Element SpawnElement() => new LocationSpawner();

        public bool IsMine(Element element) => element is LocationSpawner;

        /// <summary>
        /// Gizmos for designers showing where the spawner is oriented
        /// </summary>
        void OnDrawGizmosSelected() {
            Vector3 pos = transform.position + transform.up * 0.1f;
            var origin1 = pos + transform.right * 0.15f;
            var origin2 = pos - transform.right * 0.15f;
            var origin3 = pos + transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin1, origin3);
            Gizmos.DrawLine(origin2, origin3);
            Gizmos.DrawLine(origin1, origin2);
            if (spawnerRange > 0) Gizmos.DrawWireSphere(transform.position, spawnerRange);
        }

        
        // === Editor
#if UNITY_EDITOR
        void Awake() {
            if (Application.isPlaying) return;
            ValidatePrefabs();
        }

        void OnEnable() {
            if (!Application.isPlaying) {
                UnityEditor.Selection.selectionChanged -= TryToRegisterForUpdate;
                UnityEditor.Selection.selectionChanged += TryToRegisterForUpdate;
            }
        }

        void OnDisable() {
            UnityEditor.Selection.selectionChanged -= TryToRegisterForUpdate;
            UnityUpdateProvider.GetOrCreate().EDITOR_Unregister(this);
        }

        void TryToRegisterForUpdate() {
            if (UnityEditor.Selection.activeObject == gameObject) {
                UnityUpdateProvider.GetOrCreate().EDITOR_Register(this);
            } else {
                UnityUpdateProvider.GetOrCreate().EDITOR_Unregister(this);
            }
        }

        public void UnityEditorLateUpdate() {
            if (locationsToSpawn == null || locationsToSpawn.Length == 0) {
                return;
            }
            ValidatePrefabs();
        }

        LocationTemplateWithPrefabInstance _prefabInstance;

        void ValidatePrefabs() {
            var locationToSpawn = locationsToSpawn[0];
            bool isSet = locationToSpawn?.IsSet ?? false;
            if (isSet) {
                if (_prefabInstance.locationTemplate == locationToSpawn) {
                    if (_prefabInstance.prefabInstance == null) {
                        SpawnPrefabInstance(locationToSpawn);
                    }
                } else {
                    SpawnPrefabInstance(locationToSpawn);
                }
            } else {
                if (_prefabInstance.prefabInstance != null) {
                    GameObjects.DestroySafely(_prefabInstance.prefabInstance);
                }

                _prefabInstance = default;
            }

            foreach (Transform child in transform) {
                if (_prefabInstance.prefabInstance != child.gameObject) {
                    GameObjects.DestroySafely(child.gameObject);
                }
            }
        }

        void SpawnPrefabInstance(TemplateReference locationToSpawn) {
            const HideFlags InstanceHideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            LocationTemplate template = locationToSpawn.Get<LocationTemplate>();
            string path = UnityEditor.AssetDatabase.GetAssetPath(template);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var prefabInstance = (GameObject) UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
            LocationSpec spec = prefabInstance.GetComponent<LocationSpec>();
            if (spec != null) {
                spec.snapToGround = false;
            }
            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localRotation = Quaternion.identity;
            UnityEditor.PrefabUtility.GetPrefabInstanceHandle(prefabInstance).hideFlags = InstanceHideFlags;
            _prefabInstance = new LocationTemplateWithPrefabInstance(new TemplateReference(locationToSpawn.GUID), prefabInstance);
        }

        [Serializable]
        struct LocationTemplateWithPrefabInstance {
            public TemplateReference locationTemplate;
            public GameObject prefabInstance;

            public LocationTemplateWithPrefabInstance(TemplateReference locationTemplate, GameObject prefabInstance) {
                this.locationTemplate = locationTemplate;
                this.prefabInstance = prefabInstance;
            }
        }
#endif
    }
}
