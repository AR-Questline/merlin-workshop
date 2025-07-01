using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public partial class ArenaSpawner : Element<Location>, IHeroActionModel, IRefreshedByAttachment<ArenaSpawnerAttachment> {
        public override ushort TypeForSerialization => SavedModels.ArenaSpawner;

        const int UnitsSpacing = 3;
        
        [Saved] List<WeakModelRef<Location>> _spawnedLocations = new();
        string _spawnPoint;

        List<string> _spawnerIds;
        bool _spawnersInitialized;
        Action<ArenaSpawner> _afterSpawnersInitialized;
        IEventListener _spawnersListener;
        Vector3 _positionOffset;
        int _counter;
        int _maxUnitsInRow;
        bool _setHealthToMaxInt;

        readonly List<ManualSpawner> _spawners = new();
        [UnityEngine.Scripting.Preserve] public IEnumerable<ManualSpawner> Spawners => _spawners;
        public bool ShowSpawnAmountPopup { [UnityEngine.Scripting.Preserve] get; private set; }
        Transform SpawnPoint => ParentModel.MainView.gameObject.FindChildRecursively(_spawnPoint);

        public void InitFromAttachment(ArenaSpawnerAttachment spec, bool isRestored) {
            _spawnPoint = spec.templatesSpawnPoint.name;
            _spawnerIds = spec.spawnersParent.GetComponentsInChildren<LocationSpec>().Select(s => s.GetLocationId()).ToList();
            _maxUnitsInRow = spec.maxUnitsInRow;
            _setHealthToMaxInt = spec.setHealthToMaxInt;
            ShowSpawnAmountPopup = spec.showSpawnAmountPopup;
        }

        protected override void OnInitialize() {
            ResetPositionOffset();
            _spawnersListener = World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<BaseLocationSpawner>(), this, OnModelAdded);
            foreach (var spawner in World.All<BaseLocationSpawner>()) {
                OnModelAdded(spawner);
            }
            OnModelAdded(this);
        }

        void OnModelAdded(Model model) {
            int index = _spawnerIds.IndexOf(s => model.ID.Contains(s));
            if (index >= 0 && model is BaseLocationSpawner spawner) {
                _spawners.Add(spawner.Element<ManualSpawner>());
                _spawnerIds.RemoveAt(index);
            }

            if (!_spawnerIds.Any()) {
                _spawnersInitialized = true;
                _afterSpawnersInitialized?.Invoke(this);
                World.EventSystem.RemoveListener(_spawnersListener);
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public void OnSpawnersInitialized(Action<ArenaSpawner> callback) {
            if (_spawnersInitialized) {
                callback.Invoke(this);
            } else {
                _afterSpawnersInitialized += callback;
            }
        }

        public void SpawnNpc(LocationTemplate template) {
            if (template == null) {
                Log.Important?.Error("Trying to spawn null template, are you sure Encounters Cache is properly baked?");
                return;
            }
            
            Location loc = template.SpawnLocation(SpawnPoint.position + _positionOffset, SpawnPoint.rotation);
            if (_setHealthToMaxInt) {
                loc.OnVisualLoaded(_ => {
                    NpcElement npc = loc.TryGetElement<NpcElement>();
                    if (npc != null) {
                        npc.MaxHealth.SetTo(int.MaxValue);
                        npc.Health.SetTo(int.MaxValue);
                    }
                });
            }
            RepetitiveNpcUtils.Check(loc);
            _spawnedLocations.Add(loc);
            _positionOffset += SpawnPoint.right * UnitsSpacing;
            _counter++;
            if (_counter >= _maxUnitsInRow) {
                _positionOffset += SpawnPoint.forward * UnitsSpacing;
                _positionOffset -= SpawnPoint.right * (_counter * UnitsSpacing);
                _counter = 0;
            }
        }

        public void KillSpawned() {
            for (int i = _spawnedLocations.Count - 1; i >= 0; i--) {
                _spawnedLocations[i].Get()?.Discard();
            }
            _spawnedLocations.Clear();
            _spawners.ForEach(s => s?.ParentModel.DiscardAllSpawnedLocations());
        }

        public void ResetPositionOffset() {
            _positionOffset = Vector3.zero - SpawnPoint.right * (_maxUnitsInRow / 2f) * UnitsSpacing;
            _counter = 0;
        }
        
        // === IHeroAction
        public bool IsIllegal => false;
        public InfoFrame ActionFrame => new(DefaultActionName, true);
        public InfoFrame InfoFrame1 => new(string.Empty, false);
        public InfoFrame InfoFrame2 => new(string.Empty, false);
        public string DefaultActionName => LocTerms.Interact.Translate();

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            AddElement(new ArenaSpawnerUI());
            return true;
        }
        
        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) { }
        public void EndInteraction(Hero hero, IInteractableWithHero interactable) { }

        public ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return ActionAvailability.Available;
        }
    }
}