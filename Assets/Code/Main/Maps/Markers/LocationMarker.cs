using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Markers {
    public partial class LocationMarker : Element<Location>, ICompassMarker<CompassMarker>, IRefreshedByAttachment<MarkerAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationMarker;

        CompassMarker _spawnedCompassElement;
        CachedElement<Location, NpcElement, NpcTargetGrounded> _npcTargetGrounded;

        MutableListenerWrapper<IGrounded, Vector3> _positionListener;

        bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            private set {
                if (_enabled != value) {
                    _enabled = value;
                    this.Trigger(ICompassMarker.Events.EnabledChanged, value);
                }
            }
        }
        public ShareableSpriteReference Icon { get; private set; }
        public bool IsNumberVisible { get; private set; }
        public MarkerData MarkerData { get; private set; }
        public IMarkerDataWrapper MarkerDataWrapper { get; private set; }
        public virtual CompassMarkerType CompassMarkerType => MarkerData.CompassMarkerType;
        public CompassMarker CompassElement => _spawnedCompassElement ??= SpawnCompassMarker();
        
        public bool IgnoreDistanceRequirement => MarkerData.IgnoreDistanceRequirement;
        public bool IgnoreAngleRequirement => MarkerData.IgnoreAngleRequirement;
        public Vector3 Position { get; private set; }
        public string TooltipText => ParentModel.DisplayName;
        [UnityEngine.Scripting.Preserve] public bool IsGreyedOut => ParentModel.IsVisited || ParentModel.Cleared;
        public int OrderNumber => MarkerData.MapMarkerOrderOverride;
        protected virtual bool IsVisibleUnderFogOfWar { get; set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve] 
        public LocationMarker() { }
        
        protected LocationMarker(MarkerData data, ShareableSpriteReference icon, bool isNumberVisible = false) {
            Icon = icon;
            IsNumberVisible = isNumberVisible;
            MarkerData = data;
        }

        public void InitFromAttachment(MarkerAttachment spec, bool isRestored) {
            MarkerData data = spec.MarkerData;
            Icon = data.MarkerIcon;
            IsNumberVisible = false;
            IsVisibleUnderFogOfWar = spec.MarkerData.VisibleOnMapUnderFogOfWar;
            MarkerData = data;
            MarkerDataWrapper = spec.MarkerDataWrapper;
        }

        protected override void OnInitialize() {
            _positionListener = new MutableListenerWrapper<IGrounded, Vector3>(GroundedEvents.AfterMovedToPosition);
            _positionListener.Setup(null, pos => {
                Position = pos;
                CompassElement.LocationMoved();
            }, this);
            
            if (MarkerData.VisibleOnMap) {
                ParentModel.AddElement(
                    new PointMapMarker(
                        new WeakModelRef<IGrounded>(ParentModel),
                        () => ParentModel.DisplayName,
                        MarkerData,
                        OrderNumber,
                        IsVisibleUnderFogOfWar));
            }
            _npcTargetGrounded = new CachedElement<Location, NpcElement, NpcTargetGrounded>(this, ParentModel);
            _npcTargetGrounded.OnChanged += OnNpcTargetGroundedChanged;
            OnNpcTargetGroundedChanged(_npcTargetGrounded.Get());
            if (ParentModel is { IsStatic: false } or { Spec: { IsHidableStatic: true } }) {
                ParentModel.ListenTo(Location.Events.InteractabilityChanged, OnInteractabilityChanged, this);
            }
        }

        void OnNpcTargetGroundedChanged(NpcTargetGrounded target) {
            var grounded = target as IGrounded ?? ParentModel;
            _positionListener.ChangeSource(grounded);
            Position = grounded.Coords;
        }
        
        void OnInteractabilityChanged(LocationInteractability interactability) {
            CompassElement.OnInteractabilityChanged(interactability);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _npcTargetGrounded = null;
            _positionListener.Reset();
        }
        
        protected void SetIcon(ShareableSpriteReference icon) {
            Icon = icon;
        }
        
        public void SetEnabled(bool enabled) {
            Enabled = enabled;
        }

        protected virtual CompassMarker SpawnCompassMarker() => new(this);
    }
}