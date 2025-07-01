using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Kandra;
using Awaken.TG.Debugging;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable ArrangeAttributes

namespace Awaken.TG.Editor.Graphics.Clothes {
    public partial class ClothesTestWindow : OdinEditorWindow {
        internal const string ClothesPreviewSceneName = "ClothPreview";

        const int SpawnClothesOrder = 1;
        const int SpawnWeaponOrder = 2;
        const int CustomRigOrder = 3;
        const int AnimationsOrder = 4;
        const int CullingOrder = 5;

        ClothesTestSetup _clothesTestSetup;
        GameObject _previewInstance;
        GameObject _baseMesh;
        ARNpcAnimancer _npcAnimancer;
        Animator _animator;
        GameObject _weaponInstance;
        int _generalLayerIndex;
        ClothesCatalog _clothesCatalog;

        string[] _clothCategories;
        bool _showNewClothCategory;

        ClothesTestSetup ClothesTestSetup => _clothesTestSetup ? _clothesTestSetup : (_clothesTestSetup = FindAnyObjectByType<ClothesTestSetup>());

        // === Drawing 
        [Button, HideIf(nameof(IsRightSceneActive))]
        void OpenTestScene() {
            var scenePath =
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:scene {ClothesPreviewSceneName}").First());
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        [Button, ShowIf(nameof(CanEnterPlayMode))]
        void StartTesting() {
            EditorApplication.EnterPlaymode();
        }

        [ShowIfGroup("PlayOnly", Condition = nameof(IsPlayMode))]
        [BoxGroup("PlayOnly/Preview type"), HorizontalGroup("PlayOnly/Preview type/Horizontal")]
        [Button, DisableIf(nameof(IsSpawnedMale))]
        void SpawnMale() {
            SpawnPreview(ClothesTestSetup.malePrefab);
        }
        
        [BoxGroup("PlayOnly/Preview type"), HorizontalGroup("PlayOnly/Preview type/Horizontal")]
        [Button, DisableIf(nameof(IsSpawnedFemale))]
        void SpawnFemale() {
            SpawnPreview(ClothesTestSetup.femalePrefab);
        }
        
        [BoxGroup("PlayOnly/Spawn clothes", Order = SpawnClothesOrder), HorizontalGroup("PlayOnly/Spawn clothes/Horizontal")]
        [ShowInInspector, OnValueChanged(nameof(ClothToSpawnChanged)), LabelText("Cloth")]
        GameObject _clothToSpawn;

        [HorizontalGroup("PlayOnly/Spawn clothes/Horizontal")]
        [ShowInInspector, ValueDropdown(nameof(_clothCategories)), ShowIf(nameof(_showNewClothCategory)), LabelText("Category")]
        string _clothToSpawnCategory;
        
        [HorizontalGroup("PlayOnly/Spawn clothes/Horizontal", Width = 100)]
        [Button, DisableIf(nameof(DisableClothesSpawn))]
        void SpawnCloth() {
            if (_showNewClothCategory) {
                var categoryIndex = Array.FindIndex(_clothesCatalog.categories, c => c.name == _clothToSpawnCategory);
                ref var category = ref _clothesCatalog.categories[categoryIndex];
                ref var categoryClothes = ref category.clothes;
                Array.Resize(ref categoryClothes, categoryClothes.Length + 1);
                categoryClothes[^1] = new() {
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_clothToSpawn))
                };
                EditorUtility.SetDirty(_clothesCatalog);
                AssetDatabase.SaveAssetIfDirty(_clothesCatalog);
            }
            _showNewClothCategory = false;

            var baseRig = _baseMesh.GetComponentInChildren<KandraRig>();
            var clothInstance = ClothStitcher.Stitch(_clothToSpawn, baseRig);
            _spawnedClothes.Add(new(clothInstance));
            ClothSpawned(clothInstance, _clothToSpawn);
            _clothToSpawn = null;
        }

        [FoldoutGroup("PlayOnly/Spawn clothes/Bulk"), HorizontalGroup("PlayOnly/Spawn clothes/Bulk/Horizontal")]
        [ShowInInspector]
        GameObject[] _clothesToSpawnBulk = Array.Empty<GameObject>();

        [HorizontalGroup("PlayOnly/Spawn clothes/Bulk/Horizontal", Width = 100)]
        [Button, DisableIf(nameof(DisableBulkClothesSpawn))]
        void SpawnBulk() {
            foreach (var cloth in _clothesToSpawnBulk) {
                if (cloth) {
                    _clothToSpawn = cloth;
                    SpawnCloth();
                }
            }
            _clothToSpawn = null;
            _clothesToSpawnBulk = Array.Empty<GameObject>();
        }

        [BoxGroup("PlayOnly/Spawn clothes")]
        [ShowInInspector, ListDrawerSettings(HideAddButton = true, HideRemoveButton = false, CustomRemoveElementFunction = nameof(RemoveCloth), DraggableItems = false)]
        List<SpawnedCloth> _spawnedClothes = new();

        [BoxGroup("PlayOnly/Spawn clothes")]
        [Button]
        void DespawnAll() {
            for (int i = _spawnedClothes.Count - 1; i >= 0; i--) {
                RemoveCloth(_spawnedClothes[i]);
            }
        }
        
        [FoldoutGroup("PlayOnly/Spawn weapon", Order = SpawnWeaponOrder)]
        [Button(ButtonSizes.Small, ButtonStyle.FoldoutButton, Expanded = true), DisableIf(nameof(DisableSpawn))]
        void SpawnWeapon(GameObject prefab, [RichEnumExtends(typeof(EquipmentType)), HideReferenceObjectPicker] RichEnumReference slot) {
            if (!prefab) {
                return;
            }
            var eqType = slot.EnumAs<EquipmentType>();
            if (eqType == null) {
                return;
            }
            
            if (_weaponInstance) {
                Destroy(_weaponInstance);
            }
            Transform parent = null;
            if (eqType == EquipmentType.Bow || eqType.MainSlotType == EquipmentSlotType.OffHand) {
                parent = _previewInstance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(c => c.gameObject.CompareTag("MainHand"));
            }
            if (eqType.MainSlotType == EquipmentSlotType.MainHand) {
                parent = _previewInstance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(c => c.gameObject.CompareTag("MainHand"));
            }
            if (!parent) {
                return;
            }
            _weaponInstance = Instantiate(prefab, parent);
        }

        [ShowIfGroup("PlayOnly/CustomRig", Condition = nameof(CustomRigsEnabled), Order = CustomRigOrder)]
        [BoxGroup("PlayOnly/CustomRig/Spawn custom target")]
        [Button(ButtonSizes.Small, ButtonStyle.FoldoutButton, Expanded = true)]
        void SpawnWithCustomRig(GameObject prefab) {
            CommonSpawn(prefab);
            
            _previewInstance.AddComponent<RagdollUtilities>().RemoveRagdollFromChild();
        }

        [BoxGroup("PlayOnly/Animations", Order = AnimationsOrder)]
        [ShowInInspector, PropertyRange(0, 5)]
        float AnimatorSpeed {
            get => _npcAnimancer ? _npcAnimancer.Playable.Speed : 1;
            set {
                if (!_npcAnimancer) {
                    return;
                }
                _npcAnimancer.Playable.Speed = value;
            }
        }

        [BoxGroup("PlayOnly/Animations")]
        [ShowInInspector, PropertyRange(0, 1)]
        float AnimatorProgress {
            get => _npcAnimancer && _npcAnimancer.Layers[0].CurrentState != null ? _npcAnimancer.Layers[_generalLayerIndex].CurrentState.NormalizedTime%1f : 0;
            set {
                if (!_npcAnimancer || _npcAnimancer.Layers[0]?.CurrentState == null) {
                    return;
                }
                _npcAnimancer.Layers[0].CurrentState.NormalizedTime = value;
                if (!_npcAnimancer.Playable.IsGraphPlaying) {
                    CurrentAnimation.SampleAnimation(_npcAnimancer.gameObject, value * CurrentAnimation.length);
                }
            }
        }

        [BoxGroup("PlayOnly/Animations")]
        [ShowInInspector]
        AnimationClip CurrentAnimation {
            get {
                if (!_npcAnimancer) {
                    return null;
                }
                return _npcAnimancer.Layers[_generalLayerIndex]?.CurrentState?.Clip;
            }
        }

        [BoxGroup("PlayOnly/Animations")]
        [ShowInInspector, ListDrawerSettings(IsReadOnly = true, ShowFoldout = false), ShowIf(nameof(AnyHumanSpawned))]
        ClothesTestSetup.PredefinedAnimationClip[] _predefinedAnimations;

        [BoxGroup("PlayOnly/Animations")]
        [Button(ButtonSizes.Small, ButtonStyle.FoldoutButton, Expanded = true)]
        void ApplyCustomAnimation(AnimationClip animationClip) {
            if (!_npcAnimancer) {
                Log.Minor?.Warning("Cannot apply animation to model without Animancer");
                return;
            }
            _npcAnimancer.Play(animationClip);

            if (!_npcAnimancer.Playable.IsGraphPlaying) {
                animationClip.SampleAnimation(_npcAnimancer.gameObject, 0);
            }
        }
        
        // === Lifetime
        protected override void Initialize() {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;

            _clothesCatalog = ClothesCatalog.Instance;
        }

        protected override void OnDestroy() {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            if (_previewInstance) {
                DestroyImmediate(_previewInstance);
            }
            if (_weaponInstance) {
                GameObjects.DestroySafely(_weaponInstance);
            }
        }
        
        // === Operations
        void PlayModeChanged(PlayModeStateChange playModeStateChange) {
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode) {
                DespawnAll();
                ClearSpawnedPrefab();
                _clothesTestSetup = null;
                
                if (ClothesTestSetup) {
                    ClothesTestSetup.animations.ForEach(a => a.LoadAnimation -= ApplyCustomAnimation);
                }

                EditorApplication.update -= Repaint;
            }
            if (playModeStateChange == PlayModeStateChange.EnteredEditMode) {
                EnteredEditMode();
            }
            if (!IsRightSceneActive()) {
                return;
            }
            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode) {
                ClothesTestSetup.animations.ForEach(a => {
                    a.LoadAnimation -= ApplyCustomAnimation;
                    a.LoadAnimation += ApplyCustomAnimation;
                });
                _predefinedAnimations = ClothesTestSetup.animations;

                EditorApplication.update -= Repaint;
                EditorApplication.update += Repaint;

                EnteredPlayMode();
            }
        }

        [BoxGroup("PlayOnly/Preview type"), Button("Custom spawn", ButtonStyle.FoldoutButton)]
        void SpawnPreview(GameObject prefab) {
            CommonSpawn(prefab);
        }

        void CommonSpawn(GameObject prefab) {
            ClearSpawnedPrefab();

            if (!prefab) {
                return;
            }
            _previewInstance = Instantiate(prefab);
            _previewInstance.hideFlags = HideFlags.DontSaveInEditor;
            
            RagdollUtilities ragdollUtilities = _previewInstance.AddComponent<RagdollUtilities>() ?? prefab.GetComponent<RagdollUtilities>();
            ragdollUtilities.RemoveRagdollFromChild();
            
            _animator = _previewInstance.GetComponentInChildren<Animator>();
            _animator.applyRootMotion = false;
            _animator.enabled = true;
            _npcAnimancer = _animator.GetComponent<ARNpcAnimancer>();
            _generalLayerIndex = 0;
            _baseMesh = _animator.gameObject;
            _previewInstance.GetComponentsInChildren<Renderer>()
                            .FirstOrDefault(r => r.name.EndsWith("Face", StringComparison.InvariantCulture))
                            ?.gameObject.AddComponent<BlendShapeRandomizer>();

            if (_predefinedAnimations.Length > 0) {
                ApplyCustomAnimation(_predefinedAnimations[0].clip);
            }

            SpawnedPrefab();
        }

        void ClearSpawnedPrefab() {
            if (_previewInstance) {
                GameObjects.DestroySafely(_previewInstance);
            }
            RemovedPrefab();
            if (_weaponInstance) {
                GameObjects.DestroySafely(_weaponInstance);
            }
            _spawnedClothes.Clear();
        }

        void RemoveCloth(SpawnedCloth cloth) {
            _spawnedClothes.Remove(cloth);
            ClothRemoved(cloth.cloth);
            GameObjects.DestroySafely(cloth.cloth);
        }

        void ClothToSpawnChanged() {
            if (_clothToSpawn == null) {
                _showNewClothCategory = false;
                return;
            }

            _showNewClothCategory = !_clothesCatalog.Has(_clothToSpawn);

            if (_showNewClothCategory) {
                _clothCategories = _clothesCatalog.categories.Select(c => c.name).ToArray();
                var defaultCategoryIndex = _clothesCatalog.FindDefaultCategory(_clothToSpawn.name);
                if (defaultCategoryIndex != -1) {
                    _clothToSpawnCategory = _clothCategories[defaultCategoryIndex];
                } else if (_clothToSpawnCategory.IsNullOrWhitespace()) {
                    _clothToSpawnCategory = _clothCategories[0];
                }
            }
        }
        
        // === Enable/Disable queries
        bool IsPlayMode() => Application.isPlaying;

        bool DisableSpawn() {
            return !ClothesTestSetup || !(IsSpawnedMale() || IsSpawnedFemale());
        }
        
        bool DisableClothesSpawn() {
            return DisableSpawn() || !_clothToSpawn;
        }

        bool DisableBulkClothesSpawn() {
            return DisableSpawn() || MoreLinq.IsNullOrEmpty(_clothesToSpawnBulk);
        }

        bool IsSpawnedMale() {
            return _previewInstance &&
                   _previewInstance.TryGetComponent<NpcGenderMarker>(out var gender) &&
                   gender.Gender == Gender.Male;
        }
        
        bool IsSpawnedFemale() {
            return _previewInstance &&
                   _previewInstance.TryGetComponent<NpcGenderMarker>(out var gender) &&
                   gender.Gender == Gender.Female;
        }
        
        bool IsRightSceneActive() => SceneManager.GetActiveScene().name == ClothesTestWindow.ClothesPreviewSceneName;

        bool CanEnterPlayMode() => IsRightSceneActive() && !Application.isPlaying;

        bool CustomRigsEnabled() => TGEditorPreferences.Instance.customRigInClothesPreview;
        
        bool AnyHumanSpawned() => IsSpawnedMale() || IsSpawnedFemale();

        // === Show
        [MenuItem("TG/Assets/Clothes test", priority = 100)]
        internal static void ShowEditor() {
            var window = EditorWindow.GetWindow<ClothesTestWindow>();
            window.Show();
        }

        // === Helpers class
        [InlineProperty, HideLabel, HideReferenceObjectPicker]
        class SpawnedCloth {
            [HideLabel]
            public GameObject cloth;

            public SpawnedCloth(GameObject cloth) {
                this.cloth = cloth;
            }
        }
    }
    
    [InitializeOnLoad]
    public static class ClothesTestWindowStartup {
        static ClothesTestWindowStartup() {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }
        
        static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
            if (scene.name == ClothesTestWindow.ClothesPreviewSceneName) {
                if (!EditorWindow.HasOpenInstances<ClothesTestWindow>()) {
                    ClothesTestWindow.ShowEditor();
                }
            }
        }
    }
}
