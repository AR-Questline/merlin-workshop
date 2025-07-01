using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Regrowables;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.Housing;
using Awaken.TG.Main.UI.Housing.FurnitureSlotOverview;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using FurnitureSlotLootHandler = Awaken.TG.Main.Locations.Actions.FurnitureSlotLootHandler;
using Object = UnityEngine.Object;
using World = Awaken.TG.MVC.World;

namespace Awaken.TG.Main.Heroes.Housing {
    public abstract partial class FurnitureSlotBase : Element<Location> {
        protected FurnitureSlotAction _furnitureSlotAction;
        protected Transform _furnitureSlotTransform;
        protected TemplateReference _placeholderFurnitureReference;
        
        readonly Vector3 _interactionColliderAdditionalSize = new(0.1f, 0.1f, 0.1f);
        Location _spawnedFurniture;
        LinkedEntitiesAccess[] _linkedEntitiesAccesses;
        float _timeElapsed;
        UnsafeArray<MaterialOverrideData> _materialOverrides;
        
        [Saved] LocationTemplate _currentFurnitureTemplate;
        
        public string[] Tags { get; protected set; }
        public string DisplayName { get; protected set; }
        public FurnitureRoomAttachment FurnitureRoomAttachment { get; protected set; }
        public LocationTemplate CurrentFurnitureTemplate => _currentFurnitureTemplate;
        public Transform FurnitureLookAtTarget => _furnitureSlotAction.transform;
        
        string SlotID => $"Furniture_Slot_{ID}";
        
        protected override void OnInitialize() {
            TryToSpawnFurniturePlaceholder();
            InitListeners();
            CreateMaterialOverride();
        }
        
        protected override void OnRestore() {
            RestoreFurniture();
            InitListeners();
            CreateMaterialOverride();
            OnDecorModeStateChanged(World.HasAny<DecorMode>());
        }
        
        protected virtual void AfterVisualLoaded(Transform parentTransform, bool variantChangedByPlayer = false) {
            DelayedSetup(parentTransform, variantChangedByPlayer).Forget();
        }

        async UniTaskVoid DelayedSetup(Transform parentTransform, bool variantChangedByPlayer) {
            if (await AsyncUtil.DelayFrame(this)) {
                SetupColliderInteractionBounds(parentTransform);
                SetupFurnitureRegrowables(parentTransform);
                SetupFurnitureLoot(variantChangedByPlayer);
                CreateDecorMaterials(parentTransform);
            }
        }

        void CreateDecorMaterials(Transform parentTransform) {
            _linkedEntitiesAccesses = parentTransform.GetComponentsInChildren<LinkedEntitiesAccess>(true);
        }

        void ChangeToDecorMaterials() {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || _linkedEntitiesAccesses.IsNullOrEmpty()) {
                return;
            }
            
            MaterialsOverridePack overridePack = new(_materialOverrides);
            foreach (var linkedEntitiesAccess in _linkedEntitiesAccesses) {
                MaterialOverrideUtils.ApplyMaterialOverrides(linkedEntitiesAccess, overridePack);
            }
        }

        void RestoreOriginalMaterials() {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || _linkedEntitiesAccesses.IsNullOrEmpty()) {
                return;
            }
            
            foreach (var linkedEntitiesAccess in _linkedEntitiesAccesses) {
                MaterialOverrideUtils.RemoveMaterialOverrides(linkedEntitiesAccess, _materialOverrides[0]);
                MaterialOverrideUtils.RemoveMaterialOverrides(linkedEntitiesAccess, _materialOverrides[1]);
            }
        }

        void OnEditSlotStateChanged(bool enabled) {
            SwapDecorMaterials(!enabled);
            WithProcessUpdate(!enabled);
        }
        
        void OnDecorModeStateChanged(bool enabled) {
            if (_furnitureSlotAction) {
                _furnitureSlotAction.SetColliderActive(enabled);
            }
            
            SwapDecorMaterials(enabled);
            WithProcessUpdate(enabled);
        }

        void WithProcessUpdate(bool state) {
            if (state) {
                this.GetOrCreateTimeDependent()?.WithUpdate(ProcessUpdate);
            } else {
                this.GetOrCreateTimeDependent()?.WithoutUpdate(ProcessUpdate);
            }
        }

        void SwapDecorMaterials(bool decorMaterialsEnabled) {
            if (decorMaterialsEnabled) {
                ChangeToDecorMaterials();
            } else {
                RestoreOriginalMaterials();
            }
        }

        void SetupFurnitureLoot(bool variantChangedByPlayer) {
            if (!_spawnedFurniture.HasElement<FurnitureSearchAction>()) {
                return;
            }

            FurnitureSlotLootHandler lootHandler = TryGetElement<FurnitureSlotLootHandler>() ??
                                                   AddElement(new FurnitureSlotLootHandler());
            lootHandler.TrackLootForSpawnedFurniture(_spawnedFurniture, variantChangedByPlayer);
        }
        
        void SetupFurnitureRegrowables(Transform parentTransform) {
            var regrowableSpecs = parentTransform.GetComponentsInChildren<IRegrowableSpec>(true)
                .Select(regrowable => regrowable as SceneSpec);

            ulong index = 0;
            foreach (SceneSpec regrowableSpec in regrowableSpecs) {
                regrowableSpec.ForceSceneId(new SpecId(SlotID, 0, index, 0));
                regrowableSpec.transform.parent.gameObject.SetActive(true);
                index++;
            }
        }

        void SetupColliderInteractionBounds(Transform parentTransform) {
            if (_furnitureSlotAction == null) {
                return;
            }
            
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                Log.Important?.Error("No default world!", parentTransform.gameObject);
                return;
            }
            var entityManager = world.EntityManager;

            var accesses = parentTransform.GetComponentsInChildren<LinkedEntitiesAccess>();
            if (accesses.IsNullOrEmpty()) {
                return;
            }
            
            var filteredAccesses = accesses.Where(access => !access.GetComponent<FurnitureIgnoreVisualPartInInteractionCollider>());
            MinMaxAABB wholeBounds = MinMaxAABB.Empty;
            foreach (var access in filteredAccesses) {
                foreach (var entity in access.LinkedEntities) {
                    if (entityManager.HasComponent<WorldRenderBounds>(entity)) {
                        var worldRenderBounds = entityManager.GetComponentData<WorldRenderBounds>(entity);
                        wholeBounds.Encapsulate(worldRenderBounds.Value);
                    }
                }
            }

            if (wholeBounds.IsEmpty) {
                return;
            }

            _furnitureSlotAction.AdjustActionCollider(wholeBounds.ToBounds(), _interactionColliderAdditionalSize);
            _furnitureSlotAction.SetColliderActive(World.HasAny<DecorMode>());
        }

        void CreateMaterialOverride() {
            _materialOverrides = new UnsafeArray<MaterialOverrideData>(2, ARAlloc.Persistent) {
                [0] = new MaterialOverrideData(TypeManager.GetTypeIndex(typeof(ArchitectureBaseTintOverrideComponent)), new float4(0, 0, 0, 0)),
                [1] = new MaterialOverrideData(TypeManager.GetTypeIndex(typeof(LitBaseColorPropertyOverrideComponent)), new float4(0, 0, 0, 0))
            };
        }

        public void TryToSpawnFurnitureVariant(LocationTemplate desiredLocationTemplate, bool variantChangedByPlayer = false) {
            if (desiredLocationTemplate == _spawnedFurniture?.Template) {
                // It is already spawned
                _currentFurnitureTemplate = desiredLocationTemplate;
                return;
            }

            DespawnFurniture();
            _currentFurnitureTemplate = desiredLocationTemplate;
            SpawnFurniture(_currentFurnitureTemplate, variantChangedByPlayer);
        }
        
        void InitListeners() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<DecorMode>(), this, () => OnDecorModeStateChanged(true));
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<DecorMode>(), this, () => OnDecorModeStateChanged(false));
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<FurnitureSlotOverviewUI>(), this, () => OnEditSlotStateChanged(true));
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<FurnitureSlotOverviewUI>(), this, () => OnEditSlotStateChanged(false));
        }

        void ProcessUpdate(float deltaTime) {
            const float Duration = 2f;
            
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || _linkedEntitiesAccesses.IsNullOrEmpty()) {
                return;
            }
            
            _timeElapsed += deltaTime;
            float t = (Mathf.Sin(_timeElapsed * Mathf.PI / Duration) + 1f) / 2f;
            Color targetColor = Color.Lerp(Color.white * 2f, ARColor.DarkerGrey, t);
            SetMaterialOverrideColors();
            
            var entityManager = world.EntityManager;
            var ecb = new EntityCommandBuffer(ARAlloc.Temp);
            foreach (var linkedEntitiesAccess in _linkedEntitiesAccesses) {
                foreach (var linkedEntity in linkedEntitiesAccess.LinkedEntities) {
                    MaterialOverrideUtils.ApplyMaterialOverride(ref entityManager, linkedEntity, _materialOverrides[0], ref ecb);
                    MaterialOverrideUtils.ApplyMaterialOverride(ref entityManager, linkedEntity, _materialOverrides[1], ref ecb);
                }
            }
            ecb.Playback(entityManager);
            ecb.Dispose();
            return;

            unsafe void SetMaterialOverrideColors() {
                _materialOverrides[0].data[0] = targetColor.r;
                _materialOverrides[0].data[1] = targetColor.g;
                _materialOverrides[0].data[2] = targetColor.b;
                _materialOverrides[0].data[3] = 1f;
                _materialOverrides[1].data[0] = targetColor.r;
                _materialOverrides[1].data[1] = targetColor.g;
                _materialOverrides[1].data[2] = targetColor.b;
                _materialOverrides[1].data[3] = 1f;
            }
        }

        void DespawnFurniture(bool preserveCurrent = false) {
            _spawnedFurniture?.Discard();
            _spawnedFurniture = null;
            if (!preserveCurrent) {
                _currentFurnitureTemplate = null;
            }
        }
        
        void TryToSpawnFurniturePlaceholder() {
            if (_placeholderFurnitureReference is { IsSet: true }) {
                SpawnFurniture(_placeholderFurnitureReference.Get<LocationTemplate>());
            }
        }
        
        void RestoreFurniture() {
            if (_currentFurnitureTemplate == null) {
                if (_placeholderFurnitureReference is { IsSet: true }) {
                    SpawnFurniture(_placeholderFurnitureReference.Get<LocationTemplate>());
                }

                return;
            }

            SpawnFurniture(_currentFurnitureTemplate);
        }

        void SpawnFurniture(LocationTemplate furniture, bool variantChangedByPlayer = false) {
            _furnitureSlotTransform.GetPositionAndRotation(out var furnitureSlotPosition, out var furnitureSlotRotation);
            furniture.transform.GetPositionAndRotation(out var furniturePosition, out var furnitureRotation);

            var finalPosition = furnitureSlotPosition + furnitureSlotRotation * furniturePosition;
            var finalRotation = furnitureSlotRotation * furnitureRotation;
            _spawnedFurniture = furniture.SpawnLocation(finalPosition, finalRotation);
            _spawnedFurniture.MarkedNotSaved = true;
            _spawnedFurniture.OnVisualLoaded(parentTransform => AfterVisualLoaded(parentTransform, variantChangedByPlayer));
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _materialOverrides.Dispose();
        }
    }
    
    public abstract partial class FurnitureSlotBase<T> : FurnitureSlotBase, IRefreshedByAttachment<T> where T : FurnitureSlotAttachmentBase {
        ARAssetReference _furnitureSlotInteractionReference;
        
        public virtual void InitFromAttachment(T spec, bool isRestored) {
            _furnitureSlotTransform = spec.transform;
            Tags = spec.tags;
            DisplayName = spec.displayName;
            FurnitureRoomAttachment = spec.GetComponentInParent<FurnitureRoomAttachment>();
            _placeholderFurnitureReference = spec.furniturePlaceholder;
            
            SpawnDefaultSlotInteraction(spec);
        }

        void SpawnDefaultSlotInteraction(T spec) {
            _furnitureSlotInteractionReference = CommonReferences.Get.furnitureSlotAction.Get();
            var prefabLoading = _furnitureSlotInteractionReference.LoadAsset<GameObject>();
            prefabLoading.OnComplete(handle => {
                if (handle.Status == AsyncOperationStatus.Failed) {
                    Log.Important?.Error($"Failed to load furniture slot action prefab for {ParentModel.DisplayName}", ParentModel.ViewParent);
                    return;
                }
                
                GameObject slotActionObject = Object.Instantiate(handle.Result, spec.transform);
                slotActionObject.SetUnityRepresentation(new IWithUnityRepresentation.Options {
                    linkedLifetime = true,
                    movable = false
                });
                _furnitureSlotAction = slotActionObject.GetComponent<FurnitureSlotAction>();
                _furnitureSlotAction.AssignFurnitureSlot(this);
            });
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            _furnitureSlotInteractionReference?.ReleaseAsset();
        }
    }
}