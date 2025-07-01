using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    public partial class CrossSceneLocationMarker : Model, IGrounded {
        public override ushort TypeForSerialization => SavedModels.CrossSceneLocationMarker;

        public override Domain DefaultDomain => Domain.Gameplay;

        [Saved] public WeakModelRef<Location> Location { get; private set; }
        [Saved] public IMarkerDataTemplate Template { get; private set; }
        [Saved] public SceneReference Scene { get; private set; }
        [Saved] public bool IsFastTravel { get; private set; }
        
        [Saved] public Vector3 Coords { get; private set; }
        [Saved] public Quaternion Rotation { get; private set; }
        [Saved] public LocString DisplayName { get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve] CrossSceneLocationMarker() { }

        public CrossSceneLocationMarker(Location location, IMarkerDataTemplate markerDataTemplate, bool isFastTravel) {
            Location = new WeakModelRef<Location>(location);
            Template = markerDataTemplate;
            IsFastTravel = isFastTravel;
            var sceneService = World.Services.Get<SceneService>();
            if (location.CurrentDomain == sceneService.ActiveDomain) {
                Scene = sceneService.ActiveSceneRef;
            } else {
                throw new Exception($"Cannot create CrossSceneLocationMarker for location {location} in not active domain");
            }
        }

        protected override void OnInitialize() {
            TrySyncData();
            AddElement(new PointMapMarker(new WeakModelRef<IGrounded>(this), () => DisplayName, Template.MarkerData, MapMarkerOrder.Default.ToInt(), true));
            
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterNewDomainSet, this, OnNewDomainSet);
        }

        void OnNewDomainSet(SceneReference scene) {
            if (Scene == scene) {
                TrySyncData();
            }
        }

        void TrySyncData() {
            if (Location.TryGet(out var location)) {
                Coords = location.Coords;
                Rotation = location.Rotation;
                DisplayName = location.SpecDisplayName;
            }
        }

        public async UniTaskVoid Teleport() {
            await ScenePreloader.ChangeMapAndWait(Scene, LoadingScreenUI.Events.SceneInitializationEnded, this);
            if (Location.TryGet(out var location)) {
                if (location.TryGetElement(out LocationDiscovery discovery)) {
                    discovery.Teleport();
                } else {
                    Log.Important?.Error($"Cannot teleport. LocationDiscovery not found on location {location}");
                }
            }
        }
    }
}