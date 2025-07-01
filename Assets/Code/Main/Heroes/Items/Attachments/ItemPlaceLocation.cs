using System;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.PhysicUtils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemPlaceLocation : Element<Item>, IItemAction, IRefreshedByAttachment<ItemPlaceLocationSpec> {
        public override ushort TypeForSerialization => SavedModels.ItemPlaceLocation;

        // Placement Validation
        const float MaximumYDifference = 0.3f;
        const float PlacementNormalDotThreshold = 0.8f;
        const float AvgPlacementNormalDotThreshold = 0.9f;
        const float CriticalNormalDotThreshold = 0.7f;
        static readonly Collider[] OverlapResults = new Collider[1];
        static readonly IWithUnityRepresentation.Options UnityOptions = new IWithUnityRepresentation.Options() {
            linkedLifetime = true,
            movable = true,
        };

        ItemPlaceLocationSpec _spec;
        ItemSpawningDataRuntime _requiredMaterialForUse;
        ItemPlaceLocationHeroAction _heroAction;
        VCHeroRegionChecker _regionChecker;

        PlacementState _placementState = PlacementState.None;
        bool _performing, _heroActionPerformed;
        BlockReason _placementBlockReason;
        GameObject _placeholderValidObject, _placeholderInvalidObject;
        ARAsyncOperationHandle<GameObject> _placeholderValidObjectHandle, _placeholderInvalidObjectHandle;
        Vector3 _boundsExtents;
        Vector3[] _placementOffsetsToCheck;
        IEventListener _weaponEquipListener, _weaponChangeListener, _perspectiveChangedListener, _uiStateChangeListener;

        public ItemActionType Type => ItemActionType.Use;
        public bool IsValidPlacement => _performing && _placementState == PlacementState.Valid;
        public bool CanHeroActionBePerformed => _performing && !_heroActionPerformed;
        
        bool CanBePerformed => !_performing &&
                               !Hero.Current.IsInCombat() &&
                               (_spec.CanBePlacedInWater || !Hero.Current.IsUnderWater) &&
                               (_requiredMaterialForUse == null || Hero.Current.Inventory.HasItem(_requiredMaterialForUse, _spec.RequiredMaterialForUseCount));
        bool IsValid => ParentModel is {HasBeenDiscarded: false};
        // Prologue Check is added to handle Prologue Beach tutorial.
        bool SceneRequirementFulfilled => !_spec.OnlyOnMainScene || World.Services.Get<SceneService>() is { IsOpenWorld: true } or { IsPrologue: true } or { IsTestArena: true };

        public void InitFromAttachment(ItemPlaceLocationSpec spec, bool isRestored) {
            _spec = spec;
            if (spec.RequiredMaterialsForUse != null) {
                _requiredMaterialForUse = new(spec.RequiredMaterialsForUse);
            }
        }

        public void Submit() {
            if (!CanBePerformed) return;
            
            if (ParentModel.Owner is Hero h) {
                h.Trigger(Hero.Events.HideWeapons, true);
                _weaponEquipListener = h.ListenTo(Hero.Events.ShowWeapons, EndSpawnPlacement, this);
                _weaponChangeListener = h.HeroItems.ListenTo(HeroLoadout.Events.LoadoutChanged, EndSpawnPlacement, this);
                _perspectiveChangedListener = h.ListenTo(Hero.Events.HeroPerspectiveChanged, EndSpawnPlacement, this);
                World.Any<CharacterSheetUI>()?.Discard();
                World.Any<QuickUseWheelUI>()?.Discard();
                _heroAction = AddElement<ItemPlaceLocationHeroAction>();
                h.VHeroController.Raycaster.SetInteractionOverride(_heroAction);
                _regionChecker ??= h.VHeroController.GetComponentInChildren<VCHeroRegionChecker>();
            }
            _uiStateChangeListener = UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            
            SelectingPlaceToSpawn(World.Any<PlayerInput>()).Forget();
        }
        public void AfterPerformed() {}
        public void Perform() { }
        public void Cancel() { }

        public void HeroActionPerformed() {
            _heroActionPerformed = true;
        }

        public string GetBlockReason() {
            return _placementBlockReason switch {
                BlockReason.None => string.Empty,
                BlockReason.NotValidScene => LocTerms.PlaceOnGroundNotValidScene.Translate(),
                BlockReason.BlockedByObjects => LocTerms.PlaceOnGroundBlockedObjects.Translate(),
                BlockReason.UnevenTerrain => LocTerms.PlaceOnGroundBlockedTerrain.Translate(),
                BlockReason.TooCloseToLocations => LocTerms.PlaceOnGroundBlockedLocations.Translate(),
                BlockReason.UnderTheWater => LocTerms.PlaceOnGroundTooCloseToWater.Translate(),
                _ => string.Empty
            };
        }

        void OnUIStateChanged(UIState state) {
            if (state.IsMapInteractive) {
                return;
            }
            EndSpawnPlacement();
        }

        void SpawnLocationInFrontOfHero(Hero hero, Vector3 placement, Vector3 upwardsNormal) {
            var rotation = Quaternion.FromToRotation(Vector3.up, upwardsNormal);
            var location = _spec.LocationTemplate?.SpawnLocation(placement, rotation);
            if (_spec.PlacementVfx.IsSet) {
                PrefabPool.InstantiateAndReturn(_spec.PlacementVfx, placement, rotation).Forget();
            }
            if (location != null) {
                location.OnVisualLoaded(_ => OnLocationVisualSpawned(location));
                _requiredMaterialForUse?.ItemTemplate.ChangeQuantity(hero.Inventory, -_spec.RequiredMaterialForUseCount);
                if (_spec.ConsumeOnPlacement) {
                    ParentModel.DecrementQuantityWithoutNotification();
                }
            } else {
                EndSpawnPlacement();
            }
        }

        void OnLocationVisualSpawned(Location location) {
            EndSpawnPlacement();
            if (_spec.InteractAfterPlaced) {
                HeroInteraction.StartInteraction(Hero.Current, location, out _);
            }
        }

        async UniTaskVoid SelectingPlaceToSpawn(PlayerInput input) {
            Hero hero = Hero.Current;
            _placementState = PlacementState.None;
            _placementBlockReason = BlockReason.None;
            _performing = true;
            
            //Instantiate placeholder
            _placeholderValidObjectHandle = _spec.LocationPlaceholderValidPlacePrefab.LoadAsset<GameObject>();
            _placeholderInvalidObjectHandle = _spec.LocationPlaceholderInvalidPlacePrefab.LoadAsset<GameObject>();
            
            var validObjectResult = await _placeholderValidObjectHandle.ToUniTask();
            var invalidObjectResult = await _placeholderInvalidObjectHandle.ToUniTask();
            _placeholderValidObject = validObjectResult != null ? Object.Instantiate(validObjectResult) : null;
            _placeholderInvalidObject = invalidObjectResult != null ? Object.Instantiate(invalidObjectResult) : null;
            if (_placeholderValidObject == null || _placeholderInvalidObject == null) {
                EndSpawnPlacement();
                return;
            }
            _placeholderValidObject.SetUnityRepresentation(UnityOptions);
            _placeholderInvalidObject.SetUnityRepresentation(UnityOptions);
            CreatePlacementCheckData();
            
            while (IsValid && _performing) {
                if (HasBeenCanceled(input)) {
                    EndSpawnPlacement();
                    break;
                }
                
                Vector3 placement = GetPlacement(hero);
                PlacementState placementState = IsPlacementValid(placement, out Vector3 upwardsNormal)
                    ? PlacementState.Valid
                    : PlacementState.Invalid;
                if (HasBeenAccepted()) {
                    if (placementState == PlacementState.Valid) {
                        SpawnLocationInFrontOfHero(Hero.Current, placement, upwardsNormal);
                    } else {
                        EndSpawnPlacement();
                    }
                    break;
                }
                
                MovePlacementPlaceholder(placementState, placement, upwardsNormal);
                
                if (!await AsyncUtil.DelayFrame(hero)) {
                    EndSpawnPlacement();
                    return;
                }
            }

            if (!IsValid) {
                EndSpawnPlacement();
            }
        }

        void MovePlacementPlaceholder(PlacementState placementState, Vector3 placement, Vector3 upwardsNormal) {
            if (placementState != _placementState) {
                _placeholderValidObject.SetActive(placementState == PlacementState.Valid);
                _placeholderInvalidObject.SetActive(placementState == PlacementState.Invalid);
                _placementState = placementState;
                _heroAction.TriggerChange();
            }

            Transform placeholder = placementState == PlacementState.Valid
                ? _placeholderValidObject.transform
                : _placeholderInvalidObject.transform;
            placeholder.position = placement;
            placeholder.rotation = Quaternion.FromToRotation(Vector3.up, upwardsNormal);
        }
        
        void EndSpawnPlacement() {
            if (!_performing) {
                return;
            }
            
            _performing = false;
            _heroActionPerformed = false;
            
            _placeholderValidObjectHandle.Release();
            _placeholderInvalidObjectHandle.Release();
            if (_placeholderValidObject != null) {
                Object.Destroy(_placeholderValidObject);
            }
            if (_placeholderInvalidObject != null) {
                Object.Destroy(_placeholderInvalidObject);
            }
            
            World.EventSystem.TryDisposeListener(ref _weaponEquipListener);
            World.EventSystem.TryDisposeListener(ref _weaponChangeListener);
            World.EventSystem.TryDisposeListener(ref _perspectiveChangedListener);
            World.EventSystem.TryDisposeListener(ref _uiStateChangeListener);

            if (_heroAction != null) {
                Hero.Current.VHeroController.Raycaster.RemoveInteractionOverride(_heroAction);
                _heroAction.Discard();
                _heroAction = null;
            }
        }

        bool HasBeenAccepted() {
            if (_heroActionPerformed) {
                if (_heroAction != null) {
                    Hero.Current.VHeroController.Raycaster.RemoveInteractionOverride(_heroAction);
                    _heroAction.Discard();
                    _heroAction = null;
                }
                _heroActionPerformed = false;
                return true;
            }
            return false;
        }

        bool HasBeenCanceled(PlayerInput input) {
            return input.GetButtonDown(KeyBindings.Gameplay.Crouch) || input.GetButtonDown(KeyBindings.Gameplay.Dash) || input.GetButtonDown(KeyBindings.Gameplay.Jump);
        }

        Vector3 GetPlacement(Hero hero) {
            float angle = hero.VHeroController.FirePoint.transform.forward.y * -1f;
            float lenghtMultiplier = angle < 0f ? 1f : 1f - angle;
            lenghtMultiplier *= lenghtMultiplier;
            float length = _spec.MinimumPlacementDistance + _spec.MaximumPlacementDistance * lenghtMultiplier;
            return Ground.SnapToGround(hero.Coords + hero.Forward() * length + Vector3.up);
        }
        
        bool IsPlacementValid(Vector3 placement, out Vector3 upwardsNormal) {
            Span<Vector3> points = stackalloc Vector3[_placementOffsetsToCheck.Length];
            for (int i = 0; i < _placementOffsetsToCheck.Length; i++) {
                points[i] = Ground.SnapToGround(placement + _placementOffsetsToCheck[i]);
            }
            
            //Check normals to get avg normal and check terrain flatness
            Vector3 a = points[0];
            Span<Vector3> normals = stackalloc Vector3[points.Length - 1];
            bool dotCheckFailed = false;
            for (int i = 1; i < points.Length; i++) {
                Vector3 b = points[i];
                Vector3 c = points[i % 4 + 1];
                normals[i-1] = Vector3.Cross(c - a, b - a).normalized;
                if (Vector3.Dot(normals[i - 1], Vector3.up) < PlacementNormalDotThreshold) {
                    dotCheckFailed = true;
                }
            }
            
            upwardsNormal = (normals[0] + normals[1] + normals[2] + normals[3]) / 4;

            if (!SceneRequirementFulfilled) {
                ChangePlacementBlockReason(BlockReason.NotValidScene);
                return false;
            }
            
            float dot = Vector3.Dot(upwardsNormal, Vector3.up);
            dotCheckFailed = dotCheckFailed || dot < AvgPlacementNormalDotThreshold;
            if (dotCheckFailed) {
                ChangePlacementBlockReason(BlockReason.UnevenTerrain);
                if (dot < CriticalNormalDotThreshold) {
                    upwardsNormal = Vector3.up;
                }
                return false;
            }
            
            var boxCastPos = placement + new Vector3(0, _boundsExtents.y + MaximumYDifference, 0);
            //Check if not in the water
            if (!_spec.CanBePlacedInWater) {
                Vector3 additionalWaterDepthCheck = new(0, _boundsExtents.y * GameConstants.Get.additionalWaterDepthCheck, 0);
                foreach (var _ in PhysicsQueries.OverlapBox(boxCastPos, _boundsExtents + additionalWaterDepthCheck, _spec.WaterLayerMask)) {
                    ChangePlacementBlockReason(BlockReason.UnderTheWater);
                    return false;
                }
            }
            
            //Check if placing outside of wyrdness
            if (_spec.OutsideRepellersOnly && World.Services.Get<WyrdnessService>().IsInRepeller(boxCastPos)) {
                ChangePlacementBlockReason(BlockReason.TooCloseToLocations);
                return false;
            }
            
            if (_spec.OnlyInFactionlessTerrain) {
                foreach (var coll in PhysicsQueries.OverlapBox(boxCastPos, _boundsExtents, _regionChecker.VolumeMask)) {
                    //Check if placing in cities etc. (some ones region).
                    if (_spec.OnlyInFactionlessTerrain && coll.GetComponentInParent<SimpleCrimeRegion>()) {
                        ChangePlacementBlockReason(BlockReason.TooCloseToLocations);
                        return false;
                    }
                }
            }

            //Check if theres any collider blocking placement
            if (Physics.OverlapBoxNonAlloc(boxCastPos, _boundsExtents, OverlapResults, Quaternion.FromToRotation(Vector3.up, upwardsNormal), GameConstants.Get.obstaclesMask, QueryTriggerInteraction.Ignore) > 0) {
                ChangePlacementBlockReason(BlockReason.BlockedByObjects);
                return false;
            }

            //Check if there are locations in blocking range
            if (_spec.RequireMinimumDistanceFromCookingStations) {
                float minDistanceSqr = _spec.MinDistanceFromBlockingLocations * _spec.MinDistanceFromBlockingLocations;
                foreach (var location in World.All<Location>()) {
                    if (IsLocationToClose(location, placement, minDistanceSqr)) {
                        ChangePlacementBlockReason(BlockReason.TooCloseToLocations);
                        return false;
                    }
                }
            }

            ChangePlacementBlockReason(BlockReason.None);
            return true;

            static bool IsLocationToClose(Location loc, Vector3 placement, float minDistanceSqr) {
                if (!loc.TryGetElement<StartCraftingAction>(out var ele)) {
                    return false;
                }

                if ((loc.Coords - placement).sqrMagnitude >= minDistanceSqr) {
                    return false;
                }

                return ele.TabSetConfig.Dictionary.Keys.Contains(CraftingTabTypes.ExperimentalCooking);
            }
        }
        
        void ChangePlacementBlockReason(BlockReason reason) {
            if (_placementBlockReason != reason) {
                _placementBlockReason = reason;
                _heroAction.TriggerChange();
            }
        }
        
        void CreatePlacementCheckData() {
            _boundsExtents = _placeholderValidObject.GetComponent<Collider>().bounds.extents;
            _placementOffsetsToCheck = new [] {
                new Vector3(0, _boundsExtents.y * 2, 0),
                
                new Vector3(_boundsExtents.x, _boundsExtents.y, _boundsExtents.z),
                new Vector3(-_boundsExtents.x, _boundsExtents.y, _boundsExtents.z),
                new Vector3(-_boundsExtents.x, _boundsExtents.y, -_boundsExtents.z),
                new Vector3(_boundsExtents.x, _boundsExtents.y, -_boundsExtents.z),
            };
        }

        enum BlockReason : byte {
            None,
            BlockedByObjects,
            UnevenTerrain,
            TooCloseToLocations,
            UnderTheWater,
            NotValidScene,
        }

        enum PlacementState : byte {
            None = 0,
            Invalid = 1,
            Valid = 2
        }
    }
}