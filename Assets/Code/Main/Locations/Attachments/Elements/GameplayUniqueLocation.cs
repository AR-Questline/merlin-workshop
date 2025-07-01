using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class GameplayUniqueLocation : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.GameplayUniqueLocation;

        [Saved] string _currentScene;
        [Saved] Vector3 _currentPos;
        [Saved] Quaternion _currentRot;
        [Saved] bool _hiddenInAbyss;

        public bool InCurrentScene => !_hiddenInAbyss;
        
        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<Location, bool> ChangedAvailability = new(nameof(ChangedAvailability));
        }

        GameplayUniqueLocation() { }
        
        protected override void OnInitialize() {
            if (ParentModel.CurrentDomain != Domain.Gameplay) {
                Log.Important?.Error($"Cannot add GameplayUniqueLocation to a non-Gameplay domain location {ParentModel}.");
                Discard();
            }
            
            _hiddenInAbyss = false;
            _currentScene = World.Services.Get<SceneService>()?.ActiveSceneRef?.Name;
            _currentPos = ParentModel.SpecInitialPosition;
            _currentRot = ParentModel.SpecInitialRotation;

            if (_currentRot.Equals(new Quaternion())) {
                _currentRot = Quaternion.identity;
            }
            
            InitializeListeners();
        }

        protected override void OnRestore() {
            InitializeListeners();
        }

        void InitializeListeners() {
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneFullyInitialized, this, OnSceneEnter);
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneDiscarded, this, OnSceneDiscarded);
        }
        
        void OnSceneEnter(SceneLifetimeEventData data) {
            ChangeSceneCheck(data.SceneReference.Name);
        }
        
        void OnSceneDiscarded(SceneLifetimeEventData data) {
            if (!data.IsMainScene) {
                ChangeSceneCheck(World.Services.Get<SceneService>()?.MainSceneRef?.Name);
            }
        }

        void ChangeSceneCheck(string sceneName) {
            if (sceneName == _currentScene) {
                RestoreFromAbyss();
            } else {
                HideInAbyss();
            }
        }

        void RestoreFromAbyss() {
            if (!_hiddenInAbyss) {
                return;
            }

            ParentModel.SafelyMoveAndRotateTo(_currentPos, _currentRot);
            ParentModel.SetInteractability(LocationInteractability.Active);
            ParentModel.TryGetElement<LocationMarker>()?.SetEnabled(true);
            
            _hiddenInAbyss = false;
            ParentModel.Trigger(Events.ChangedAvailability, true);
        }

        void HideInAbyss() {
            if (_hiddenInAbyss) {
                return;
            }

            _currentPos = ParentModel.SavedCoords;
            _currentRot = ParentModel.SavedRotation;
            ParentModel.SafelyMoveTo(NpcPresence.AbyssPosition);
            ParentModel.SetInteractability(LocationInteractability.Hidden);
            ParentModel.TryGetElement<LocationMarker>()?.SetEnabled(false);
            
            _hiddenInAbyss = true;
            ParentModel.Trigger(Events.ChangedAvailability, false);
        }
        
        public void TeleportIntoCurrentScene(Vector3 position) {
            _currentScene = World.Services.Get<SceneService>()?.ActiveSceneRef?.Name;
            _currentPos = position;

            if (_hiddenInAbyss) {
                RestoreFromAbyss();
            } else {
                ParentModel.SafelyMoveTo(_currentPos);
            }
        }

        public static void InitializeForLocation(Location location) {
            if (!location.HasElement<GameplayUniqueLocation>()) {
                location.MoveToDomain(Domain.Gameplay);
                location.AddElement(new GameplayUniqueLocation());
            }
        }
    }
}