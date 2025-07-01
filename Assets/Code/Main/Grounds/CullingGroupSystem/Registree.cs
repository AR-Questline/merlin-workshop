using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    /// <summary>
    /// Registree: The actor that is being registered
    /// </summary>
    public sealed class Registree {
        const int InvalidDistanceBand = -1;
        static readonly UniversalProfilerMarker Update = new("Registree: Update");

        BaseCullingGroup _cullingGroup;
        ICullingSystemRegistreeModel _cullingGroupModel;

        public ICullingSystemRegistree Parent { get; private set; }
        public float Radius { get; private set; }
        
        public int CurrentDistanceBand { get; private set; } = int.MaxValue;
        public int ScheduledDistanceBand { get; private set; } = InvalidDistanceBand;
        
        public int RegistrarIndex { get; private set; }
        public bool IsRegistered { get; private set; }
        
        public Vector3 Coords => Parent.Coords;
        

        // === Public Methods
        public void RegisterSelf() {
            if (IsRegistered) return;
            _cullingGroup.Register(this);
            IsRegistered = true;
        }

        public void UnregisterSelf() {
            if (!IsRegistered) return;
            _cullingGroup.Unregister(this);
            ScheduledDistanceBand = InvalidDistanceBand;
            IsRegistered = false;
        }

        public bool ScheduleUpdate(int newDistanceBand) {
            bool unscheduled = ScheduledDistanceBand == InvalidDistanceBand;
            ScheduledDistanceBand = newDistanceBand; 
            return unscheduled;
        }
        
        public void UpdateDistanceBand() {
            if (ScheduledDistanceBand == InvalidDistanceBand) {
                return;
            }

            if (CurrentDistanceBand == ScheduledDistanceBand
                || !IsRegistered) { // This check may trigger when a callback modifies registrar
                ScheduledDistanceBand = InvalidDistanceBand;
                return;
            }

            // Apply new distance band
            CurrentDistanceBand = ScheduledDistanceBand;
            ScheduledDistanceBand = InvalidDistanceBand;

            Update.Begin();
            // Send updates
            Parent.CullingSystemBandUpdated(CurrentDistanceBand);

            _cullingGroupModel?.Trigger(ICullingSystemRegistreeModel.Events.DistanceBandChanged, CurrentDistanceBand);
            Update.End();
        }

        public void TriggerPausedEvent(bool paused) {
            _cullingGroupModel?.Trigger(ICullingSystemRegistreeModel.Events.DistanceBandPauseChanged, paused);
        }

        public void SetRegistrarIndex(int index) {
            RegistrarIndex = index;
        }

        public void UpdateOwnPosition() {
            if (IsRegistered) {
                _cullingGroup.UpdatePosition(this, Coords);
            }
        }
        
        // === Constructors
        Registree() { }
        
        [MustUseReturnValue("You must call build to finalize")]
        public static Builder<TGroup> ConstructFor<TGroup>(ICullingSystemRegistree owner) where TGroup : BaseCullingGroup {
            return new(owner, World.Services.Get<CullingSystem>().GetCullingGroupInstance<TGroup>());
        }

        // === Functionality containers

        public sealed class Builder<TGroup> where TGroup : BaseCullingGroup {
            Registree _registree;

            /// <summary>
            /// Create a Registree
            /// </summary>
            public Builder(ICullingSystemRegistree owner, BaseCullingGroup group) {
                _registree = new Registree {
                    _cullingGroup = group,
                    Radius = 1,
                    Parent = owner,
                    IsRegistered = false,
                    _cullingGroupModel = owner as ICullingSystemRegistreeModel,
                };
            }

            // === Registree settings
            /// <summary>
            /// Overrides default radius of 1
            /// </summary>
            [MustUseReturnValue("You must call build to finalize")] [UnityEngine.Scripting.Preserve]
            public Builder<TGroup> WithRadius(float radius) {
                _registree.Radius = radius;
                return this;
            }

            /// <summary>
            /// Finalizes the creation of the Registree
            /// </summary>
            [MustUseReturnValue("Registree does not inherently do anything. Must be sent to culling system to activate")]
            public Registree Build() {
                var local = _registree;
                _registree = null;
                return local;
            }
        }
    }
}
