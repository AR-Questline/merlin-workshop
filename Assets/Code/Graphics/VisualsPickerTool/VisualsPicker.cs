#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.EditorOnly;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using System;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.VisualsPickerTool {
    [ExecuteInEditMode]
    public class VisualsPicker : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR
        [SerializeField, PropertyOrder(-10)] bool snapToGround = true;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, true, AddressableGroup.Locations)] ARAssetReference defaultPrefab;
        [SerializeField, Space(20), ReadOnly] GameObject currentVisuals;
        [SerializeField, Space(20), OnValueChanged(nameof(RefreshAssetGroup)), HideLabel, EnumToggleButtons] VisualsKitTypes visualKit;
        [SerializeField, HideInInspector] VisualsKitTypes previousVisualKit;
        
        [SerializeField, HideInInspector] List<VisualsPickerGroup> assetGroups = new();
        [SerializeField, HideInInspector] int currentIndex = -1;

        public VisualsKitTypes VisualKit => visualKit;
        public GameObject CurrentVisuals => currentVisuals;

        public GameObject EDITOR_GameObject { get; private set; }

        /// <summary>
        /// This field is only for easy list-editing in prefab mode, it's serialization is useless but it's required for nice drawing
        /// </summary>
        [SerializeField, HideLabel]
        // ReSharper disable once NotAccessedField.Local
        VisualsPickerGroup currentGroup;
        public VisualsPickerGroup CurrentGroup => currentGroup = GetPickerInPrefab().FindOrCreateAssetGroup(visualKit.ToString());

        public int CurrentIndex {
            get => currentIndex;
            private set {
                currentIndex = value;
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Used for holding a non serialized index when we want to change source prefab values instead of this instance values
        /// </summary>
        int _tempIndex = -1;

        // === Lifetime events
        void OnEnable() {
            if (Application.isPlaying) {
                return;
            }
            EDITOR_GameObject = gameObject;
            UnityUpdateProvider.GetOrCreate().EDITOR_Register(this);

            if (!IsOnScene()) {
                CheckIfStaticSpawned();
            }
        }
        
        public void UnityEditorSelectedLateUpdate() {
            if (IsOnScene()) {
                CheckIfStaticSpawned();
            }
            if (snapToGround) {
                Vector3 position = Ground.SnapToGround(transform.position, transform);
                if (position != transform.position) {
                    transform.position = position;
                    EditorUtility.SetDirty(gameObject);
                }
            }
        }

        void OnDisable() {
            UnityUpdateProvider.GetOrCreate().EDITOR_Unregister(this);
        }
        
        void CheckIfStaticSpawned() {
            if (CanSpawnAssets() && gameObject.isStatic && currentVisuals == null && defaultPrefab != null) {
                RefreshCurrentPrefab();
            }
        }

        // === Main refresh logic
        /// <summary>
        /// General system refresh with handling for prefab inconsistencies and cleaning up of garbage instances
        /// </summary>
        /// <param name="force">Should visual be regenerated even if correct and exists</param>
        [Button, ContextMenu("RefreshPrefab"), PropertyOrder(-1)]
        public void RefreshCurrentPrefab(bool force = false) {
            VisualsPicker assetVisualPicker = FindDeepestSavedVisualsPicker();

            // Check for inconsistent states between this instance and the corresponding object in asset
            if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) // Checks if we are not in root prefab
                && assetVisualPicker != null
                && assetVisualPicker.currentVisuals != null // Ignore unset states
                && (!EditorPrefabHelpers.IsTheSameObject(assetVisualPicker.currentVisuals, currentVisuals) // Check if visuals are the same as prefab values
                    || !PrefabUtility.IsAnyPrefabInstanceRoot(assetVisualPicker.currentVisuals))) {
                CleanupWrongInstancesAndLoadFromPrefab(assetVisualPicker: assetVisualPicker);
                return;
            }
            
            if (currentVisuals != null) {
                EditorPrefabHelpers.RemoveAllRedundantInstances(transform, currentVisuals); // Cleanup garbage
            }

            // Handle invalid index
            if (_tempIndex < 0 || _tempIndex >= CurrentGroup.Count) {
                if (CanSpawnAssets() && (currentVisuals == null || PrefabUtility.IsAddedGameObjectOverride(currentVisuals))) {
                    _tempIndex = CurrentIndex;
                }
            }
            // The actual refresh
            if (force || currentVisuals == null) {
                SetVisuals(GetCurrentGroupAsset());
                // This is done in the case where the reference was missing and we only have one once we generate a new instance
                if (currentVisuals != null) {
                    EditorPrefabHelpers.RemoveAllRedundantInstances(transform, currentVisuals); // Cleanup garbage
                }
            }
            
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(gameObject);
        }

        /// <summary>
        /// Expensive, should not be called unless prefab -> instance desync detected
        /// </summary>
        void CleanupWrongInstancesAndLoadFromPrefab(VisualsPicker assetVisualPicker) {
            if (currentVisuals != null) {
                EditorPrefabHelpers.DestroyAnywhere(currentVisuals);
            }

            // Cleanup non prefab instance clones
            if (assetVisualPicker.currentVisuals != null &&
                !PrefabUtility.IsAnyPrefabInstanceRoot(assetVisualPicker.currentVisuals)) {
                assetVisualPicker.RefreshCurrentPrefab(true);
            }

            currentVisuals = assetVisualPicker.currentVisuals;
            CurrentIndex = assetVisualPicker.CurrentIndex;
            PrefabUtility.RevertObjectOverride(this, InteractionMode.AutomatedAction);
            _tempIndex = CurrentIndex;
            EditorPrefabHelpers.RemoveAllRedundantInstances(transform, currentVisuals); // Cleanup garbage
        }

        /// <summary>
        /// Gets the last visual picker with a non null current visual in prefab chain
        /// </summary>
        VisualsPicker FindDeepestSavedVisualsPicker() {
            VisualsPicker assetVisualPicker = this;
            while (true) {
                VisualsPicker prefabVisual = PrefabUtility.GetCorrespondingObjectFromSource(assetVisualPicker);
                if (prefabVisual != null && prefabVisual.currentVisuals != null) {
                    assetVisualPicker = prefabVisual;
                } else {
                    break;
                }
            }

            return assetVisualPicker;
        }

        /// <summary>
        /// For use within asset editing only
        /// </summary>
        public void SetCurrentVisual(int index) {
            CurrentIndex = index;
            _tempIndex = index;
            if (currentVisuals) {
                GameObjects.DestroySafely(currentVisuals);
            }
            SetVisuals(GetCurrentGroupAsset());
        }

        // === Main Visual logic
        void SetVisuals(ARAssetReference visuals) {
            if (currentVisuals != null) {
                EditorPrefabHelpers.RemoveAllRedundantInstances(transform, currentVisuals);
            }
            if (visuals is not { IsSet: true }) {
                return;
            }

            if (!TrySetDynamicVisuals(visuals)) { 
                SetStaticVisuals(visuals);
            }
            
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(gameObject);
        }

        bool TrySetDynamicVisuals(ARAssetReference visuals) {
            if (gameObject.isStatic) {
                return false;
            }

            LocationSpec parentLocation = GetComponent<LocationSpec>();
            if (parentLocation == null) {
                return false;
            }

            parentLocation.prefabReference = visuals;
            parentLocation.ValidatePrefab(true);
            CurrentIndex = _tempIndex;
            EditorUtility.SetDirty(parentLocation);
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(gameObject);
            return true;
        }

        void SetStaticVisuals(ARAssetReference visuals) {
            GameObject prefab = visuals.LoadAsset<GameObject>().WaitForCompletion();
            if (prefab == null) {
                visuals.ReleaseAsset();
                return;
            }
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
            PrefabUtility.GetPrefabInstanceHandle(instance).hideFlags |= HideFlags.NotEditable;
            visuals.ReleaseAsset();
            if (instance == null) return;
            
            // Easy removal when not prefab bound object
            if (currentVisuals != null && EditorPrefabHelpers.InstantiatedHere(currentVisuals)) {
                GameObjects.DestroySafely(currentVisuals);
            }
            
            // Bind loaded instance
            if (currentVisuals == null) {
                currentVisuals = instance;
                instance.transform.localPosition = Vector3.zero;
                if (instance.isStatic) {
                    GameObjects.SetStaticRecursively(instance, true);
                }

                CurrentIndex = _tempIndex;
                return;
            }

            // sets visual in prefab as it doesn't exist here
            SetPrefabVisual(currentVisuals, _tempIndex);
            GameObjects.DestroySafely(instance);

        }
        
        public static void SetPrefabVisual(GameObject objToReplace, int newIndex) {
            EditorPrefabHelpers.DoForObjectInSourceAsset(objToReplace, p => {
                var vp = p.transform.parent.GetComponent<VisualsPicker>();
                if (vp != null) {
                    vp.SetCurrentVisual(newIndex);
                }
            }, SourceAssetSearchMode.VisualPickers);
        }

        // === Helper properties
        public bool CanChangeAssets() => IsAssetGroupSet() && CanSpawnAssets();

        public bool CanSpawnAssets() => IsOnScene() || PrefabUtility.IsAnyPrefabInstanceRoot(gameObject);

        bool IsOnScene() => gameObject.scene.IsValid() && StageUtility.GetStage(gameObject.scene) is not PrefabStage;

        bool IsDefaultPrefabSet() => defaultPrefab != null;

        public bool IsAssetGroupSet() => CurrentGroup is {HasAssets: true};
        
        // === UI
        [Button("<"), ButtonGroup("arrows"), ShowIf(nameof(CanChangeAssets))]
        public void PreviousVisual() {
            DecreaseIndex();
            SetVisuals(GetCurrentGroupAsset());
        }
        
        [Button(">"), ButtonGroup("arrows"), ShowIf(nameof(CanChangeAssets))]
        public void NextVisual() {
            IncreaseIndex();
            SetVisuals(GetCurrentGroupAsset());
        }

        [Button, ShowIf(nameof(IsDefaultPrefabSet)), ShowIf(nameof(CanSpawnAssets)), PropertyOrder(-5)]
        void ResetToDefault() {
            SetIndex(-1);
            SetVisuals(defaultPrefab);
        }

        [UnityEngine.Scripting.Preserve] public static bool isRefreshing = false;
        [Button("RefreshAllVisualPrefabs"), ShowIf(nameof(CanSpawnAssets)), PropertyOrder(-10)]
        void RefreshAllVisualPrefabs() {
            AssetDatabase.StartAssetEditing();
            isRefreshing = true;
            Stage stage = StageUtility.GetStage(gameObject.scene);
            foreach (var picker in stage.FindComponentsOfType<VisualsPicker>()) {
                try {
                    picker.RefreshCurrentPrefab();
                } catch (Exception e) {
                    Log.Important?.Error($"Exception below happened for {picker.gameObject.name}", picker.gameObject);
                    Debug.LogException(e);
                }
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            isRefreshing = false;
        }
        [Button, PropertyOrder(-10)]
        void VerifyNoDuplicates() {
            isRefreshing = true;
            Stage stage = StageUtility.GetStage(gameObject.scene);
            foreach (var picker in stage.FindComponentsOfType<VisualsPicker>()) {
                try {
                    picker.VerifyNoDuplicatesHere();
                } catch (Exception e) {
                    Log.Important?.Error($"Exception below happened for {picker.gameObject.name}", picker.gameObject);
                    Debug.LogException(e);
                }
            }
            isRefreshing = false;
        }

        void VerifyNoDuplicatesHere() {
            foreach (Transform child in transform) {
                if (child.gameObject != currentVisuals && !child.name.Contains("Interaction") && !child.name.Contains("DamageZone")) {
                    Log.Important?.Warning("Possible invalid Visual Picker Prefab. Found: " + child.name + " when looking for: " + currentVisuals.name, child);
                }
            }
        }
        
        // === Editor UI helpers
        public void SetByIndex(int index) {
            SetIndex(index);
            SetVisuals(GetCurrentGroupAsset());
        }

        public void DecreaseIndex() {
            _tempIndex = CurrentIndex - 1;
            if (_tempIndex < 0) {
                _tempIndex = CurrentGroup.Count - 1;
            }
        }
        
        public void IncreaseIndex() {
            _tempIndex = CurrentIndex + 1;
            if (_tempIndex >= CurrentGroup.Count) {
                _tempIndex = 0;
            }
        }

        public void SetIndex(int index) {
            _tempIndex = index;
        }

        public void RefreshAssetGroup() {
            if (visualKit != previousVisualKit) {
                previousVisualKit = visualKit;
                _tempIndex = -1;
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(gameObject);
            }
        }
        
        public void AddNewAsset(ARAssetReference assetReference) {
            GetPickerInPrefab().FindOrCreateAssetGroup(visualKit.ToString()).AddAsset(assetReference);
        }

        ARAssetReference GetCurrentGroupAsset() {
            try {
                if (CurrentGroup.Count == 0 || _tempIndex == -1) return defaultPrefab;
                return CurrentGroup.GetAssetReference(_tempIndex);
            } catch (Exception e) {
                Debug.LogException(e, gameObject);
                return null;
            }
        }
        
        VisualsPickerGroup FindOrCreateAssetGroup(string visualKit) {
            for (int i = assetGroups.Count - 1; i>= 0; i--) {
                if (Enum.GetNames(typeof(VisualsKitTypes)).All(e => e != assetGroups[i].OwnerKit)) {
                    assetGroups.RemoveAt(i);
                }
                
                if (assetGroups[i].OwnerKit == visualKit) {
                    return assetGroups[i];
                }
            }

            var newGroup = new VisualsPickerGroup(visualKit);
            assetGroups.Add(newGroup);
            return newGroup;
        }

        VisualsPicker GetPickerInPrefab() {
            var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(this);
            if (original == null) {
                original = this;
            }

            return original;
        }
#endif
    }
}
