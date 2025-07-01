using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using MagicaCloth2;
using Pathfinding;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.Locations.Views {
    [UsesPrefab("Locations/VSpawnedLocation")]
    public class VSpawnedLocation : VLocation, IVLocationWithState {
        protected Transform _modelInstanceTransform;
        protected bool? _isVisible = null;

        ReferenceInstance<GameObject> _spawnedReference;
        bool _queueStateUpdate;
        Bounds _modelBounds;

        Action _lateUpdate;

        public GameObject ModelInstance { get; private set; }

        string DebugString => $"{Target.DisplayName} ({(this != null ? name : string.Empty)}) - {_spawnedReference?.Reference.RuntimeKey}";

        // === Initialization
        public override Transform DetermineHost() => Target.ViewParent;

        protected override void OnInitialize() {
            OnInitializedAsync().Forget();
        }

        async UniTaskVoid OnInitializedAsync() {
            bool result = await AsyncUtil.DelayFrameOrTime(gameObject, 3, 150);
            if (HasBeenDiscarded) {
                return;
            }
            if (!result) {
                Target.VisualLoadingFailed();
                return;
            }
            
            base.OnInitialize();
            //spawn prefab
            Target.AfterFullyInitialized(() => OnAfterFullyInitialized(Target.SavedCoords, Target.SavedRotation));
            gameObject.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                linkedLifetime = true, movable = !Target.IsNonMovable,
            });
            // hide if necessary
            InitVisibility();
            Target.ListenTo(Model.Events.AfterChanged, UpdateState, this);
        }
        
        void InitVisibility() {
            UpdateState();
        }

        // === Operations
        void OnAfterFullyInitialized(Vector3 position, Quaternion rotation) {
            ClearReferences();
            ARAssetReference prefabReference = Target.CurrentPrefab.DeepCopy();
            
            try {
                _spawnedReference = new ReferenceInstance<GameObject>(prefabReference);
                Target.MoveAndRotateTo(position, rotation);
                _spawnedReference.Instantiate(transform, position, rotation, g => SetupPrefabAsync(g).Forget(), LoadingCancelled);
            } catch (Exception e) {
                Log.Important?.Error($"Exception below happened for location: {Target.DisplayName}", gameObject);
                Debug.LogException(e);
                Target.VisualLoadingFailed();
                ClearReferences();
            }
        }

        void LoadingCancelled() {
            if (HasBeenDiscarded) {
                return;
            }
            Target.VisualLoadingFailed();
            ClearReferences();
        }

        async UniTaskVoid SetupPrefabAsync(GameObject modelInstance) {
            if (modelInstance) {
                InitialPrefabSetup(modelInstance);
            } else {
                Log.Important?.Error($"Failed to load location prefab for {DebugString}", this);
                Target.VisualLoadingFailed();
                ClearReferences();
                return;
            }

            // Npc can spawn stuffs before scene Culling system is added.
            // Because NPC can be at Domain.Gameplay but CullingSystem is scene bound
            bool success = await AsyncUtil.WaitUntil(this, () => World.Services.TryGet<CullingSystem>() != null);
            if (HasBeenDiscarded) {
                return;
            }
            if (!success) {
                Log.Important?.Error($"Location view destroyed from outside, it shouldn't happen. Location: {DebugString}", this);
                return;
            }

            ModelInstance = modelInstance;
            _modelInstanceTransform = ModelInstance.transform;
            
            SetupPrefabAfterPositionSet().Forget();
            OnLocationReady();
        }

        void InitialPrefabSetup(GameObject modelInstance) {
            modelInstance.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                linkedLifetime = true, movable = !Target.IsNonMovable,
            });
                
            if (modelInstance.TryGetComponent(out RichAI richAI)) {
                richAI.enabled = false;
            }

            if (Target.HasElement<NpcElement>()) {
                Target.SetCulled(true);
                Hide();
            }
        }

        async UniTaskVoid SetupPrefabAfterPositionSet() {
            if (this == null) {
                return;
            }

            Target.Trigger(Location.Events.BeforeVisualLoaded, ModelInstance);

            NpcElement isNPC = Target.TryGetElement<NpcElement>();
            if (isNPC is { IsVisualSet: true }) {
                var visual = await isNPC.LoadVisual(ModelInstance);
                if (this == null || Target == null) {
                    return;
                }
                if (visual != null) {
                    visual.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                        linkedLifetime = true, movable = !Target.IsNonMovable,
                    });
                    foreach (MagicaCloth magicaChildren in visual.GetComponentsInChildren<MagicaCloth>(true)) {
                        magicaChildren.Process.Init();
                    }
                }
            }

            if (_queueStateUpdate) {
                UpdateState();
                _queueStateUpdate = false;
            }
            
            InitializeViewComponents(ModelInstance.transform);
            VSTriggerOnVisualLoaded();
            if (ModelInstance.TryGetComponent(out RichAI richAI)) {
                richAI.enabled = true;
            }
            Target.VisualLoaded(ModelInstance.transform, LocationVisualSource.FromPrefab);
        }

        // === Updating
        public void UpdateState() {
            if (ModelInstance != null) {
                bool shouldBeVisible = Target.VisibleToPlayer && Target.IsCulled != true;
                if (!shouldBeVisible && _isVisible != false) {
                    Hide();
                } else if (shouldBeVisible && _isVisible != true) {
                    Show();
                }
            } else {
                _queueStateUpdate = true;
            }
        }

        // === Discarding
        protected override IBackgroundTask OnDiscard() {
            foreach (var view in GetComponentsInChildren<IView>(true)) {
                if (ReferenceEquals(view, this) || !view.IsInitialized || view.HasBeenDiscarded) {
                    continue;
                }

                try {
                    view.Discard();
                } catch (Exception e) {
                    Log.Important?.Error($"Exception below happened for view: {view.GetType().FullName} on object: {view.gameObject.name}");
                    Debug.LogException(e);
                }
            }
            ClearReferences();
            return base.OnDiscard();
        }

        // === Helpers
        
        void Show() {
            _isVisible = true;
            for (int i = 0; i < Target.ViewParent.childCount; i++) {
                Target.ViewParent.GetChild(i).gameObject.SetActive(true);
            }
            gameObject.SetActive(true);
            Target.Trigger(Location.Events.LocationVisibilityChanged, true);
            OnVisibilityChanged();
        }

        void Hide() {
            _isVisible = false;
            Target.Trigger(Location.Events.LocationVisibilityChanged, false);
            for (int i = 0; i < Target.ViewParent.childCount; i++) {
                Target.ViewParent.GetChild(i).gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
            OnVisibilityChanged();
        }

        public void ClearReferences() {
            ModelInstance = null;
            _modelInstanceTransform = null;
            _spawnedReference?.ReleaseInstance();
            _spawnedReference = null;
            OnClearReferences();
        }

        // === Destroy
        void OnDestroy() {
            ClearReferences();
        }

        protected virtual void OnClearReferences() {}
        protected virtual void OnLocationReady() {}
        protected virtual void OnVisibilityChanged() {}
    }
}
