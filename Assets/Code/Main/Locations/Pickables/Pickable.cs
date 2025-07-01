using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations.Pickables {
    public class Pickable : IInteractableWithHero, IHeroAction, ICrimeSource {
        readonly PickableSpecBase _spec;
        
        GameObject _spawnedRegrowablePart;
        ItemSpawningDataRuntime _itemData;
        
        public SpecId Id => _spec.Id;

        ItemTemplate Template => _spec.ItemData.ItemTemplate(_spec);
        
        public Vector3 Coords => _spec.transform.position;
        [UnityEngine.Scripting.Preserve] public Scene Scene => _spec.gameObject.scene;
        
        public bool Interactable => IsValidAction;
        public string DisplayName => Template.ItemName;
        public GameObject InteractionVSGameObject => null;
        public Vector3 InteractionPosition => Coords;
        public bool IsIllegal => Crime.Theft(_itemData, this).IsCrime();

        public bool IsValidAction => _itemData is { ItemTemplate: { } };
        public InfoFrame ActionFrame => new(DefaultActionName, true);
        public InfoFrame InfoFrame1 => new(string.Empty, false);
        public InfoFrame InfoFrame2 => new(string.Empty, false);
        
        public string DefaultActionName => (IsIllegal ? LocTerms.Steal : LocTerms.Pickup).Translate();

        public Pickable(PickableSpecBase spec) {
            _spec = spec;
        }

        public void Initialize(PickableService pickableService) {
            if (!pickableService.WasPicked(this)) {
                if (_spec.TryLoadPrefab(out var prefabLoading)) {
                    prefabLoading.OnComplete(OnPrefabLoaded);
                    _itemData = _spec.ItemData.ToRuntimeData(_spec);
                }
            }
        }

        public void Uninitialize() {
            DespawnVisuals();
        }

        public void NotifyPicked() {
            World.Services.Get<PickableService>().NotifyPicked(this);
            DespawnVisuals();
        }

        void OnPrefabLoaded(ARAsyncOperationHandle<GameObject> handle) {
            if (handle.Status == AsyncOperationStatus.Failed) {
                Log.Minor?.Error($"Failed to load pickable asset! {_spec}", _spec);
                return;
            }
            
            _spawnedRegrowablePart = Object.Instantiate(handle.Result, _spec.transform);
            _spawnedRegrowablePart.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                linkedLifetime = true,
                movable = false,
            });
        }
        
        void DespawnVisuals() {
            if (_spawnedRegrowablePart) {
                Object.Destroy(_spawnedRegrowablePart);
                _spawnedRegrowablePart = null;
            }
            if (_spec.TryGetValidPrefab(out var prefabReference)) {
                prefabReference.ReleaseAsset();
                _itemData = null;
                return;
            }
            _itemData = null;
        }
        
        Vector3 ICrimeSource.Position => _spec.transform.position;

        CrimeOwnerTemplate ICrimeSource.DefaultOwner => _spec.CrimeOwner;
        Faction ICrimeSource.Faction => null;

        bool ICrimeSource.IsNoCrime(in CrimeArchetype archetype) => !_spec.IsCrime;
        ref readonly CrimeArchetype ICrimeSource.OverrideArchetype(in CrimeArchetype archetype) => ref archetype;
        float ICrimeSource.GetBountyMultiplierFor(in CrimeArchetype archetype) => 1;

        public IEnumerable<IHeroAction> AvailableActions(Hero hero) {
            yield return this;
        }
        public IHeroAction DefaultAction(Hero hero) => this;

        public void DestroyInteraction() => throw new Exception("Pickable cannot be destroyed");

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            var item = new Item(_itemData);
            World.Add(item);

            if (item.TryGetElement(out ItemRead itemRead)) {
                item.AddElement(new ItemBeingPicked(this));
                itemRead.Submit();
            } else {
                CommitCrime.Theft(item, this);
                hero.Inventory.Add(item);
                NotifyPicked();
            }
            
            return true;
        }
        
        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) { }

        public void EndInteraction(Hero hero, IInteractableWithHero interactable) { }

        public ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return IsValidAction ? ActionAvailability.Available : ActionAvailability.Disabled;
        }
    }
}