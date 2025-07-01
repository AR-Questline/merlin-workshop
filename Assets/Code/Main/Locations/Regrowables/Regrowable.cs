using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations.Regrowables {
    public class Regrowable : IInteractableWithHero, IHeroAction, ICullingSystemRegistree, ICrimeSource {
        // === static cache
        static Dictionary<SpecId, Regrowable> s_regrowableById = new();
        public static bool TryGetById(SpecId id, out Regrowable regrowable) => s_regrowableById.TryGetValue(id, out regrowable);

        // === instance data
        readonly uint _localId;
        readonly IRegrowableSpec _spec;
        GameObject _spawnedRegrowablePart;

        bool _canRegrow;
        ItemSpawningDataRuntime _regrownItemData;
        AsyncOperationHandle<GameObject> _regrowableLoadingOperationHandle;

        public SpecId Id => _spec.MVCId(_localId);
        
        public ItemTemplate Template => _spec.ItemReference(_localId).ItemTemplate(null);
        public TimeSpan RegrowRate => _spec.RegrowRate(_localId);

        public Vector3 Coords => _spec.Transform(_localId).position;

        public bool Interactable => IsValidAction;
        public string DisplayName => Template.ItemName;
        public GameObject InteractionVSGameObject => _spec.gameObject;
        public Vector3 InteractionPosition => Coords;
        public bool IsIllegal => Crime.Theft(_regrownItemData, this).IsCrime();

        public bool IsValidAction => _regrownItemData is { ItemTemplate: not null};
        
        public InfoFrame ActionFrame => new(DefaultActionName, true);
        public InfoFrame InfoFrame1 => new(string.Empty, false);
        public InfoFrame InfoFrame2 => new(string.Empty, false);
        
        public string DefaultActionName => (IsIllegal ? LocTerms.Steal : LocTerms.Pickup).Translate();

        Vector3 ICrimeSource.Position => Coords;

        CrimeOwnerTemplate ICrimeSource.DefaultOwner => _spec.CrimeOwner(_localId);
        Faction ICrimeSource.Faction => null;

        bool ICrimeSource.IsNoCrime(in CrimeArchetype archetype) => false;
        ref readonly CrimeArchetype ICrimeSource.OverrideArchetype(in CrimeArchetype archetype) => ref archetype;
        float ICrimeSource.GetBountyMultiplierFor(in CrimeArchetype archetype) => 1;

        public Regrowable(uint localId, IRegrowableSpec spec) {
            _localId = localId;
            _spec = spec;
        }

        public void Initialize(RegrowableService regrowableService) {
            s_regrowableById.Add(Id, this);
            if (!regrowableService.IsRegrowing(this)) {
                Spawn();
            }
        }

        public void Uninitialize() {
            s_regrowableById.Remove(Id);
            // Discard only if Spawn was called
            if (_regrowableLoadingOperationHandle.IsValid()) {
                DiscardVisual();
            }
        }

        // === Operations
        public bool TryRegrow() {
            if (!_canRegrow) {
                return false;
            }
            Spawn();
            return true;
        }

        void Spawn() {
            var key = _spec.RegrowablePartKey(_localId);
            if (_regrowableLoadingOperationHandle.IsValid()) {
                Log.Important?.Error($"Double loading of regrowable asset [{key}]! {this}", _spec as MonoBehaviour);
                DiscardVisual();
            }
            _regrowableLoadingOperationHandle = Addressables.LoadAssetAsync<GameObject>(key);
            _regrowableLoadingOperationHandle.Completed += OnPrefabLoaded;
            _regrownItemData = _spec.ItemReference(_localId).ToRuntimeData(this);
        }
        
        void OnPrefabLoaded(AsyncOperationHandle<GameObject> _) {
            if (_regrowableLoadingOperationHandle.IsValid() == false) { // Canceled
                return;
            }
            if (_regrowableLoadingOperationHandle.Status != AsyncOperationStatus.Succeeded) {
                Log.Minor?.Error($"Failed to load regrowable asset! {this}", _spec as MonoBehaviour);
                DiscardVisual();
                return;
            }

            _spawnedRegrowablePart = Object.Instantiate(_regrowableLoadingOperationHandle.Result, _spec.transform);
            var partTransform = _spawnedRegrowablePart.transform;
            var transform = _spec.Transform(_localId);
            partTransform.SetPositionAndRotation(transform.position, transform.rotation);
            partTransform.localScale = (float3)transform.scale;
            _spawnedRegrowablePart.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                linkedLifetime = true,
                movable = false,
            });

            _spec.RegrowablePartSpawned(_localId, _spawnedRegrowablePart);
        }

        void DespawnRegrowablePart() {
            World.Services.Get<RegrowableService>().Register(this);
            _regrownItemData = null;
            DiscardVisual();
        }

        void DiscardVisual() {
            if (_spawnedRegrowablePart) {
                _spec.RegrowablePartDespawned(_localId);
                Object.Destroy(_spawnedRegrowablePart);
            }
            if (!_regrowableLoadingOperationHandle.IsValid()) {
                Log.Important?.Error($"Double unloading of regrowable asset! {this}", _spec as MonoBehaviour);
                return;
            }
            _regrowableLoadingOperationHandle.Release();
            _regrowableLoadingOperationHandle = default;
        }

        // === ICullingSystemRegistree
        public void CullingSystemBandUpdated(int newDistanceBand) {
            _canRegrow = newDistanceBand > 0;
        }

        public Registree GetRegistree() {
            return Registree.ConstructFor<RegrowableCullingGroup>(this).Build();
        }

        public IEnumerable<IHeroAction> AvailableActions(Hero hero) {
            yield return this;
        }

        public IHeroAction DefaultAction(Hero hero) {
            return this;
        }

        public void DestroyInteraction() {
            throw new Exception("Regrowables cannot be destroyed");
        }

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            Pickup();
            return true;
        }

        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) { }
        
        public void EndInteraction(Hero hero, IInteractableWithHero interactable) { }

        public ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return IsValidAction ? ActionAvailability.Available : ActionAvailability.Disabled;
        }

        [UnityEngine.Scripting.Preserve]
        public bool Equals(Regrowable other) => ReferenceEquals(this, other);

        void Pickup() {
            if (_regrownItemData == null) {
                return;
            }
            
            var item = new Item(_regrownItemData);
            World.Add(item);
            if (Hero.Current.Development.CanGatherAdditionalPlants) {
                item.IncrementQuantity();
            }
            CommitCrime.Theft(item, this);
            Hero.Current.Inventory.Add(item);
            DespawnRegrowablePart();
            
            // Trigger VS
            VGUtils.SendCustomEvent(InteractionVSGameObject, null, VSCustomEvent.Interact);
            if (_spec.StoryOnPickedUp is { IsValid: true }) {
                Story.StartStory(StoryConfig.Base(_spec.StoryOnPickedUp, null));
            }
        }

        public override string ToString() {
            return $"{nameof(Regrowable)}: {Id}[{_localId}] [{_spec}]";
        }
    }
}
