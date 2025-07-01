using Awaken.TG.Assets;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.MVC;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Compasses {
    public partial class CompassMarker : CompassElement, ICullingSystemRegistreeModel {
        public sealed override bool IsNotSaved => true;

        bool _alwaysVisible;
        GameConstants _gameConstants;
        bool _enabled;
        Registree _registree;

        public override bool ShouldBeDisplayed => Enabled && CalculateEnabled;
        public override ShareableSpriteReference Icon => Marker.Icon;
        public override string TopText => Marker.TooltipText;
        public override int OrderNumber => Marker.OrderNumber;
        public override bool IsNumberVisible => Marker.IsNumberVisible;
        public bool ShowDistance { get; private set; }

        protected LocationMarker Marker { get; }
        bool Interactable => Marker.ParentModel.Interactability != LocationInteractability.Hidden;
        protected virtual bool CalculateEnabled => MarkerEnabled && Interactable && !TooFarAway;
        
        float NearDistanceSq { get; set; }
        float FarDistanceSq { get; set; }
        float FactorMultiplier { get; set; }
        bool TooFarAway { get; set; }
        bool MarkerEnabled { get; set; }
        
        CompassMarkerData MarkerData(CompassMarkerType type) => _gameConstants.MapMarkersData[type];

        public CompassMarker(LocationMarker marker) : base(
            enabled: marker.Enabled, 
            ignoreDistanceRequirement: marker.IgnoreDistanceRequirement, 
            ignoreAngleRequirement: marker.IgnoreAngleRequirement
        ) {
            Marker = marker;
            MarkerEnabled = Marker.Enabled;
        }

        protected override void OnInitialize() {
            _gameConstants = Services.Get<GameConstants>();
            OnTypeChanged(Marker.CompassMarkerType);
            
            Marker.ListenTo(Model.Events.BeforeDiscarded, Discard, this);
            Marker.ListenTo(ICompassMarker.Events.EnabledChanged, OnEnabledChanged, this);
            Marker.ListenTo(ICompassMarker.Events.TypeChanged, OnTypeChanged, this);
        }

        void OnEnabledChanged(bool enabled) {
            MarkerEnabled = enabled;
            UpdateVisibility();
        }
        
        public void OnInteractabilityChanged(LocationInteractability interactability) {
            UpdateVisibility();
        }

        void OnTypeChanged(CompassMarkerType type) {
            var data = MarkerData(type);
            var nearSq = data.nearDistance * data.nearDistance;
            var farSq = data.farDistance * data.farDistance;
            FactorMultiplier = 1 / (nearSq - farSq) * (1 - VCompassElement.MinFactorValue);
            NearDistanceSq = nearSq;
            FarDistanceSq = farSq;
        }

        // === Public API
        public void UpdateVisibility() {
            if (IsDisplayed != ShouldBeDisplayed) {
                this.Trigger(Events.StateChanged, this);
            }
        }
        
        public void LocationMoved() => _registree?.UpdateOwnPosition();

        public virtual float Distance(Vector3 from) => Marker.Position.DistanceTo(from);

        public void SetShowDistance(bool state) {
            ShowDistance = state;
        }

        public void UpdateIcon() {
            this.Trigger(Events.IconUpdated, true);
        }
        
        // === ICompassElement
        public override Vector3 Direction(Vector3 from) {
            return Marker.Position - from;
        }

        public override AlphaValue CalculateAlpha(Vector3 observer) {
            if (IgnoreDistanceRequirement) {
                return AlphaValue.FullyOpaque;
            }
            float distanceSq = (Marker.Position - observer).sqrMagnitude;
            if (distanceSq >= FarDistanceSq) {
                return AlphaValue.FullyTransparent;
            } else if (distanceSq <= NearDistanceSq) {
                return AlphaValue.FullyOpaque;
            } else {
                // InverseLerp(farSq, nearSq, distanceSq).Remap(0, 1, MinFactor, 1);
                return AlphaValue.Blended(VCompassElement.MinFactorValue + (distanceSq - FarDistanceSq) * FactorMultiplier);
            }
        }
        
        // === ICullingSystemRegistreeModel
        public Vector3 Coords => Marker.Position;
        public Quaternion Rotation => Quaternion.identity;

        public Registree GetRegistree() => _registree = Registree.ConstructFor<CompassMarkerCullingGroup>(this).Build();

        public void CullingSystemBandUpdated(int newDistanceBand) {
            bool notTooFarAway = IgnoreDistanceRequirement || CompassMarkerCullingGroup.MarkerActive(MarkerData(Marker.CompassMarkerType).farDistance, newDistanceBand);
            bool tooFarAway = !notTooFarAway;
            if (tooFarAway != TooFarAway) {
                TooFarAway = tooFarAway;
                UpdateVisibility();
            }
        }
    }
}