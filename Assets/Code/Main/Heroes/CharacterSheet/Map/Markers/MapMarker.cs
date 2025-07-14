using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public abstract partial class MapMarker : Element<IModel> {
        public sealed override bool IsNotSaved => true;

        WeakModelRef<IGrounded> _groundedRef;
        Func<string> _displayNameGetter;
        public Vector3 Position => Grounded.Coords;
        public string DisplayName => _displayNameGetter();
        public int Order { get; }
        public bool Rotate { get; }
        public bool IsAlwaysVisible { get; }
        public bool UseHighlightAnimation { get; }

        public IGrounded Grounded => _groundedRef.Get();
        
        protected abstract Type ViewType { get; }

        protected MapMarker(WeakModelRef<IGrounded> groundedRef, Func<string> displayNameGetter, int order, bool isAlwaysVisible, bool rotate = false, bool highlightAnimation = false) {
            _groundedRef = groundedRef;
            _displayNameGetter = displayNameGetter;
            Order = order;
            IsAlwaysVisible = isAlwaysVisible;
            Rotate = rotate;
            UseHighlightAnimation = highlightAnimation;
        }

        public new static class Events {
            public static readonly Event<MapMarker, IGrounded> PositionChanged = new(nameof(PositionChanged));
        }

        protected override void OnInitialize() {
            var grounded = Grounded;
            grounded.ListenTo(GroundedEvents.AfterMoved, OnGroundedMoved, this);
            grounded.ListenTo(GroundedEvents.AfterTeleported, OnGroundedMoved, this);
            grounded.ListenTo(Model.Events.BeforeDiscarded, _ => Discard(), this);
        }
        
        public bool IsFromScene(SceneReference scene) {
            return ParentModel switch {
                CrossSceneLocationMarker crossSceneLocationMarker => crossSceneLocationMarker.Scene == scene,
                Hero hero => World.Services.Get<SceneService>().ActiveSceneRef == scene,
                _ => true,
            };
        }

        public void SpawnView(MapSceneUI mapSceneUI) {
            var view = World.SpawnView(this, ViewType);
            ((IVMapMarker)view).Init(mapSceneUI);
            mapSceneUI.ListenTo(Model.Events.BeforeDiscarded, _ => view.Discard(), this);
        }

        void OnGroundedMoved(IGrounded grounded) {
            this.Trigger(Events.PositionChanged, grounded);
        }
    }
    
    public enum MapMarkerOrder : byte { 
        Default = 0,
        Quest = 1,
        TrackedQuest = 2,
        Hero = 3,
        CustomMarker = 4,
    }
}