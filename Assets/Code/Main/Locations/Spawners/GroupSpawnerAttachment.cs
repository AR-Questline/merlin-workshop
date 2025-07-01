using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners {
    [ExecuteInEditMode]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Spawns NPCs in a group. Can handle all cases of spawning.")]
    public class GroupSpawnerAttachment : SpawnerAttachment, IAttachmentSpec {
        [Space]
        [LabelText("Discard On Full Clear"), Tooltip("The spawner is discarded after the last spawned enemy is defeated."), DisableIf(nameof(discardAfterSpawn))]
        public bool discardAfterAllKilled;
        [LabelText("Spawn Once (and discard immediately)"), Tooltip("Use this when you want to spawn something once and don't need to handle any further logic, like running STORY ON ALL KILLED"), DisableIf(nameof(discardAfterAllKilled)), DisableIf(nameof(discardSpawnedLocationsOnDiscard))]
        public bool discardAfterSpawn;
        
        [DisableIf("@" + nameof(discardAfterSpawn))]
        public bool overrideSpawnerCooldown;
        /// <summary>
        /// this name is no longer correct. check label override
        /// </summary>
        [LabelText("@" + nameof(CooldownLabel)), DisableIf(nameof(discardAfterSpawn)), HideIf(nameof(overrideSpawnerCooldown))]
        [Tooltip("Cooldown in seconds, counted in real time of playing, not influenced by resting.")]
        public float cooldownAfterAllKilled = DefaultSpawnerCooldown;
        [DisableIf("@" + nameof(discardAfterSpawn) + " || " + nameof(discardAfterAllKilled)), PropertySpace(spaceBefore: 0, spaceAfter: 10)]
        public bool mustFullClearToRespawn;

        // Two different modes, static allowing exact placement. And advanced allowing fine control of randomized group spawning
        [EnumToggleButtons, SerializeField, BoxGroup("Spawn Mode", centerLabel: true), PropertySpace(spaceBefore: 0, spaceAfter: 10)]
        [OnValueChanged(nameof(OnSpawnModeChanged))]
        SpawnMode spawnMode;
        
        [SerializeField, HideIf(nameof(AdvancedSpawning), animate: false), BoxGroup("Spawn Mode", showLabel: false), 
         LabelText("Locations"), InfoBox("Add spec to list then move spawned specs to set position for spawn")]
        List<LocationTemplateWithPosition> locationsWithPositions = new();
        
        [SerializeField, InlineProperty, HideLabel] 
        [ShowIf(nameof(AdvancedSpawning), false), BoxGroup("Spawn Mode")] 
        SpawnerRandomizationSettings randomizationSettings = new();

        [SerializeField, HideInInspector] int currentIndex;
        int GetNextIndex() => ++currentIndex;

        public IEnumerable<LocationTemplateWithPosition> LocationsToSpawn => locationsWithPositions;
        public float SpawnerCooldown => overrideSpawnerCooldown ? cooldownAfterAllKilled : DefaultSpawnerCooldown;

        public SpawnerRandomizationSettings RandomizationSettings => AdvancedSpawning ? randomizationSettings : null;
        
        public Element SpawnElement() {
            return new GroupLocationSpawner();
        }

        public bool IsMine(Element element) => element is GroupLocationSpawner;

        [Serializable]
        public class LocationTemplateWithPosition {
            [InfoBox("Cannot be unique npc", InfoMessageType.Error, nameof(NotRepetitiveNpc))]
            [TemplateType(typeof(LocationTemplate)), HideLabel]
            public TemplateReference locationToSpawn;
            [HideInInspector] public int id;
            [HideInInspector] public Matrix4x4 locationMatrix;
            
            public LocationTemplate LocationToSpawn => locationToSpawn?.Get<LocationTemplate>();
            
            bool NotRepetitiveNpc => RepetitiveNpcUtils.InvalidLocation(locationToSpawn);
        }

        // === Editor
        
        // -- Odin
        enum SpawnMode {
            Static,
            Advanced
        }
        bool StaticSpawning => spawnMode == SpawnMode.Static;
        bool AdvancedSpawning => spawnMode == SpawnMode.Advanced;
        string CooldownLabel => StaticSpawning ? "Spawn Cooldown After All Killed" : "Spawn Cooldown After Group Killed";
        protected override bool ShowStoryOnAllKilled => !discardAfterSpawn;
        
        void OnSpawnModeChanged() {
#if UNITY_EDITOR
            if (Application.isPlaying) {
                return;
            }

            if (AdvancedSpawning) {
                UnityUpdateProvider.GetOrCreate().EDITOR_Unregister(this);
            } else {
                UnityUpdateProvider.GetOrCreate().EDITOR_Register(this);
            }
#endif
        }
        
        // -- Preview
#if UNITY_EDITOR
        Dictionary<int, LocationTemplateWithPrefabInstance> _prefabInstances = new();

        void OnEnable() {
            if (Application.isPlaying || AdvancedSpawning) {
                return;
            }
            UnityUpdateProvider.GetOrCreate().EDITOR_Register(this);
        }

        void OnDisable() {
            UnityUpdateProvider.GetOrCreate().EDITOR_Unregister(this);
        }

        public void UnityEditorLateUpdate() {
            if (Selection.activeGameObject != gameObject &&
                (Selection.activeTransform == null || !Selection.activeTransform.IsChildOf(transform))) {
                return;
            }

            ValidatePrefabs();
            UpdateLocationsPositions();
        }

        void ValidatePrefabs() {
            bool isOnScene = !string.IsNullOrWhiteSpace(gameObject.scene.name) && UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null;
            if (isOnScene && StaticSpawning) {
                foreach (var loc in locationsWithPositions) {
                    var locationToSpawn = loc.locationToSpawn;
                    bool isSet = locationToSpawn?.IsSet ?? false;
                    if (!isSet) { continue; }

                    if (loc.id <= 0) {
                        loc.id = GetNextIndex();
                    }

                    if (_prefabInstances.ContainsKey(loc.id)) {
                        var entry = _prefabInstances[loc.id];
                        if (entry.locationTemplate != locationToSpawn || entry.prefabInstance == null) {
                            if (entry.prefabInstance != null) {
                                GameObjects.DestroySafely(entry.prefabInstance);
                            }
                            _prefabInstances.Remove(loc.id);
                            SpawnPrefabInstance(loc.id, loc);
                        }
                    } else {
                        SpawnPrefabInstance(loc.id, loc);
                    }
                }

                foreach ((int index, LocationTemplateWithPrefabInstance value) in _prefabInstances.ToArray()) {
                    if (locationsWithPositions.All(t => t.id != index)) {
                        GameObjects.DestroySafely(value.prefabInstance);
                        _prefabInstances.Remove(index);
                    }
                }
            } else {
                foreach (var value in _prefabInstances.Values) {
                    GameObjects.DestroySafely(value.prefabInstance);
                }
                _prefabInstances.Clear();
            }

            foreach (Transform child in transform) {
                if (_prefabInstances.Values.All(e => e.prefabInstance != child.gameObject)) {
                    GameObjects.DestroySafely(child.gameObject);
                }
            }
        }

        void SpawnPrefabInstance(int index, LocationTemplateWithPosition locationWithPosition) {
            TemplateReference locationToSpawn = locationWithPosition.locationToSpawn;
            LocationTemplate template = locationToSpawn.Get<LocationTemplate>();
            string path = AssetDatabase.GetAssetPath(template);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var prefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(prefab, transform);
            prefabInstance.transform.localPosition = locationWithPosition.locationMatrix.ExtractPosition();
            prefabInstance.transform.localRotation = locationWithPosition.locationMatrix.ExtractRotation();
            PrefabUtility.GetPrefabInstanceHandle(prefabInstance).hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            _prefabInstances.Add(index, new LocationTemplateWithPrefabInstance(new TemplateReference(locationToSpawn.GUID), prefabInstance));
        }

        void UpdateLocationsPositions() {
            foreach ((int index, LocationTemplateWithPrefabInstance value) in _prefabInstances.ToArray()) {
                var entry = locationsWithPositions.FirstOrDefault(l => l.id == index);
                Matrix4x4 currentPos = value.prefabInstance.transform.LocalTransformToMatrix();
                if (entry != null) {
                    if (entry.locationMatrix != currentPos) {
                        entry.locationMatrix = currentPos;
                        EditorUtility.SetDirty(gameObject);
                        EditorUtility.SetDirty(this);
                    }
                } else {
                    _prefabInstances.Remove(index);
                }
            }
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

        void OnDrawGizmosSelected() {
            if (StaticSpawning) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, RandomizationSettings.spawnRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, RandomizationSettings.groupSpawnRadius);
        }
#endif
    }
}