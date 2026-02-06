using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.UI.TitleScreen.Loading;
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

        static string CurrentScene => World.Services.Get<SceneService>()?.ActiveSceneRef?.Name;
        
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
            _currentScene = CurrentScene;
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
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, OnSceneChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<LoadingScreenUI>(), this, OnLoadingStarted);
        }
        
        void OnSceneChanged(SceneLifetimeEvents _) {
            ChangeSceneCheck(CurrentScene);
        }

        void OnLoadingStarted(Model _) {
            HideInAbyss();
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
            SetCurrentScene(CurrentScene);
            _currentPos = position;

            if (_hiddenInAbyss) {
                RestoreFromAbyss();
            } else {
                ParentModel.SafelyMoveTo(_currentPos);
            }
        }

        public void SetCurrentScene(string sceneName) {
            _currentScene = sceneName;
        }
        
        public void SetCurrentPosition(Vector3 position) {
            _currentPos = position;
        }
        
        public void TeleportToGameplayUniqueLocation(GameplayUniqueLocation other) {
            if (!other.InCurrentScene) {
                HideInAbyss();
                _currentScene = other._currentScene;
                _currentPos = other._currentPos;
                _currentRot = other._currentRot;
            } else if (other.InCurrentScene && !InCurrentScene) {
                _currentScene = other._currentScene;
                _currentPos = other.ParentModel.Coords;
                _currentRot = other.ParentModel.Rotation;
                RestoreFromAbyss();
            }
            
            else if (other.InCurrentScene && InCurrentScene) {
                ParentModel.SafelyMoveAndRotateTo(other.ParentModel.Coords, other.ParentModel.Rotation);
            }
        }

        public void HideCompletely() {
            _currentScene = null;
            HideInAbyss();
        }

        public static GameplayUniqueLocation InitializeForLocation(Location location) {
            if (!location.HasElement<GameplayUniqueLocation>()) {
                location.MoveToDomain(Domain.Gameplay);
                return location.AddElement(new GameplayUniqueLocation());
            }

            return location.Element<GameplayUniqueLocation>();
        }
    }
}