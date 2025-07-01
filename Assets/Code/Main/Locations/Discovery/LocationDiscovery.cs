using Awaken.TG.Main.ActionLogs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Discovery {
    public partial class LocationDiscovery : AbstractLocationAction, IRefreshedByAttachment<LocationDiscoveryAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationDiscovery;

        [Saved] public bool Discovered { get; private set; }

        IAreaPrimitive[] _primitives;
        Bounds? _bounds;
        string _flag;
        float _expMulti;
        GameObject _specGO;
        bool _wasOutside = true;
        JournalGuid _journalEntryGuid;

        public bool IsFastTravel { get; private set; }
        public Vector3 FastTravelPoint { get; private set; }
        [UnityEngine.Scripting.Preserve] public MarkerData MarkerData => ParentModel.Element<LocationMarker>().MarkerData;
        public override string DefaultActionName => LocTerms.FastTravelPopupTitle.Translate();

        public new static class Events {
            public static readonly Event<LocationDiscovery, LocationDiscovery> LocationEntered = new(nameof(LocationEntered));
            public static readonly Event<LocationDiscovery, LocationDiscovery> LocationExited = new(nameof(LocationExited));
            public static readonly Event<LocationDiscovery, Location> LocationDiscovered = new(nameof(LocationDiscovered));
        }
        
        public void InitFromAttachment(LocationDiscoveryAttachment spec, bool isRestored) {
            _specGO = spec.gameObject;
            _flag = spec.UnlockFlag;
            // set flag if it was added in patch after location had been discovered
            if (Discovered && !_flag.IsNullOrWhitespace()) {
                StoryFlags.Set(_flag, true);
            }

            _expMulti = spec.ExpMulti;
            _journalEntryGuid = spec.guid;
            IsFastTravel = spec.IsFastTravel;
            FastTravelPoint = spec.FastTravelLocation;
        }

        protected override void OnInitialize() {
            // add tag to location so that berlin can use it in quest markers etc.
            ParentModel.AddTag(_flag);
            SetupPrimitives(_specGO);
        }

        protected override void OnRestore() {
            SetupPrimitives(_specGO);
        }

        protected override void OnFullyInitialized() {
            World.Services.Get<UnityUpdateProvider>().RegisterLocationDiscovery(this);
        }

        public void UnityUpdate(Hero hero) {
            Vector3 heroCoords = hero.Coords;
            if (Contains(heroCoords)) {
                if (!Discovered) {
                    Discover();
                }

                if (_wasOutside) {
                    this.Trigger(Events.LocationEntered, this);
                    _wasOutside = false;
                }
            } else {
                if (!_wasOutside) {
                    this.Trigger(Events.LocationExited, this);
                }

                _wasOutside = true;
            }
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) =>
            IsFastTravel ? ActionAvailability.Available : ActionAvailability.Disabled;

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (IsFastTravel) {
                CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Map, availableTabs: CharacterSheetTabType.MapOnlyTabs);
                World.Only<MapUI>().AllowFastTravel();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Services.Get<UnityUpdateProvider>().UnregisterLocationDiscovery(this);
        }

        bool Contains(in Vector3 point) {
            if (_bounds.HasValue && !_bounds.Value.Contains(point)) {
                return false;
            }
            
            for (int i = 0; i < _primitives.Length; i++) {
                if (_primitives[i].Contains(point)) {
                    return true;
                }
            }
            return false;
        }

        public void Discover() {
            Discovered = true;
            OnDiscover();
            this.Trigger(Events.LocationDiscovered, ParentModel);
            World.Only<PlayerJournal>().UnlockEntry(_journalEntryGuid.GUID, JournalSubTabType.Lore);
        }

        void OnDiscover() {
            if (!_flag.IsNullOrWhitespace()) {
                StoryFlags.Set(_flag, true);
            }

            AnnounceDiscovery(
                IsFastTravel ? LocTerms.FastTravelPoint.Translate() : LocTerms.NewLocationDiscovered.Translate(),
                ParentModel.DisplayName
            );

            Hero.Current.Development.RewardExpAsPercentOfNextLevel(_expMulti);
            CreateMarker(); 
        }

        void AnnounceDiscovery(string discoveryTitle, string discoveryMessage) {
            var discoveryData = new NewLocationNotificationData(ParentModel, discoveryTitle, discoveryMessage, _expMulti, IsFastTravel ? CommonReferences.Get.FastTravelIcon : null);
            AdvancedNotificationBuffer.Push<LocationDiscoveryBuffer>(new NewLocationNotification(discoveryData));
        }

        void SetupPrimitives(GameObject spec) {
            var providers = spec.GetComponentsInChildren<IAreaPrimitiveProvider>();
            _primitives = new IAreaPrimitive[providers.Length];
            for (int i = 0; i < providers.Length; i++) {
                _primitives[i] = providers[i].Spawn();
            }

            if (_primitives.Length == 0) {
                Log.Important?.Error("LocationDiscoverAttachment has no PrimitiveProviders", spec);
            } else if (_primitives.Length > 1) {
                var bounds = _primitives[0].Bounds;
                for (int i = 1; i < _primitives.Length; i++) {
                    bounds.Encapsulate(_primitives[i].Bounds);
                }

                _bounds = bounds;
            }
        }

        void CreateMarker() {
            if (ParentModel.TryGetElement(out LocationMarker locationMarker)) {
                var template = locationMarker.MarkerDataWrapper.Template;
                if (template != null) {
                    World.Add(new CrossSceneLocationMarker(ParentModel, template, IsFastTravel));
                } else {
                    Log.Important?.Error($"Location {ParentModel.DisplayName} has LocationMarker with embedded MarkerData in MarkerAttachment. For LocationDiscovery use explicit.");
                }
            } else {
                Log.Important?.Error($"Location {ParentModel.DisplayName} has no LocationMarker. For LocationDiscovery attach MarkerAttachment to Location Spec.");
            }
        }

        public void Teleport() {
            var characterSheetUI = World.Any<CharacterSheetUI>();
            characterSheetUI?.DiscardWithoutTransition();
            var hero = Hero.Current;
            Portal.FastTravel.To(hero, FastTravelPoint).Forget();
        }
    }
}