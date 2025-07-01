using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using Awaken.Utility.Extensions;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VCHeroTrespassingChecker : ViewComponent<Hero>, IVolumeChecker {
        [SerializeField] LayerMask volumeMask;
        [SerializeField] string volumeTag;

        RegionTracker _tracker;

        protected override void OnAttach() {
            _tracker = new(Target);
            Target.ListenTo(GroundedEvents.TeleportRequested, OnTeleportRequested, this);
            Target.ListenTo(CrimeUtils.Events.RecalculateTrespassing, RecalculateTrespassing, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.FlagChanged, this, OnFlagChanged);
        }

        void RecalculateTrespassing() {
            _tracker.Clear();
            Physics.OverlapSphere(transform.position, 0.1f, volumeMask, QueryTriggerInteraction.Collide).ForEach(OnTriggerEnter);
        }

        void OnTriggerEnter(Collider other) {
            GameObject otherGameObject = other.gameObject;
            if (volumeMask.Contains(otherGameObject.layer) && (volumeTag.IsNullOrWhitespace() || otherGameObject.CompareTag(volumeTag))) {
                Enter(other);
            }
        }

        void OnTriggerExit(Collider other) {
            if (_tracker.RemoveRegion(other)) {
                var region = other.GetComponentInParent<CrimeRegion>();
                if (region != null) {
                    _tracker.RemoveFlag(region.EnablingFlag);
                }
            }
        }

        void Update() {
            if (_tracker.AnyRegistered) {
                _tracker.RemoveNulls();
            }
        }

        void Enter(Collider other) {
            var factionOwnedRegion = other.GetComponentInParent<CrimeRegion>();
            if (factionOwnedRegion == null) {
                Log.Important?.Error("FactionOwnedRegion is null!");
                return;
            }

            if (!factionOwnedRegion.Enabled) {
                return;
            }

            bool addedSuccessfully;
            if (factionOwnedRegion.IsSafeRegion) {
                addedSuccessfully = _tracker.AddAllowedRegion(other);
            } else {
                addedSuccessfully = _tracker.AddTrespassingRegion(other);
            }

            if (addedSuccessfully) {
                _tracker.AddFlag(factionOwnedRegion.EnablingFlag);
            }
        }

        void OnTeleportRequested() {
            _tracker.Clear();
            Target.ListenToLimited(GroundedEvents.AfterTeleported, OnAfterTeleported, this);
        }
        
        void OnAfterTeleported() {
            Physics.OverlapSphere(transform.position, 0.1f, volumeMask, QueryTriggerInteraction.Collide).ForEach(OnTriggerEnter);
        }

        void OnFlagChanged(string flag) {
            _tracker.OnFlagChanged(flag);
        }

        class RegionTracker {
            static List<Collider> s_updatedColliders = new();
            HashSet<Collider> _trespassingRegions = new();
            HashSet<Collider> _allowedRegions = new();
            List<string> _enablingFlags = new();
            [UnityEngine.Scripting.Preserve] Hero _target;
            TrespassingTracker _trespassingTracker;

            public RegionTracker(Hero target) {
                _target = target;
                _trespassingTracker = target.Element<TrespassingTracker>();
            }
            
            public bool AnyRegistered => _trespassingRegions.AnyNonAlloc() || _allowedRegions.AnyNonAlloc();
            public int TrespassingCount => _trespassingRegions.Count;
            public int AllowedCount => _allowedRegions.Count;

            public bool IsTrespassing => TrespassingCount > 0 && AllowedCount == 0;

            void UpdateState(bool previousState) {
                var currentState = IsTrespassing;
                if (currentState != previousState) {
                    _trespassingTracker.Trigger(TrespassingTracker.Events.TrespassingVolumeChanged, currentState);
                }
            }

            public bool AddAllowedRegion(Collider c) {
                var previousState = IsTrespassing;
                if (_allowedRegions.Add(c)) {
                    UpdateState(previousState);
                    return true;
                }
                return false;
            }

            public bool AddTrespassingRegion(Collider c) {
                var previousState = IsTrespassing;
                if (_trespassingRegions.Add(c)) {
                    UpdateState(previousState);
                    return true;
                }
                return false;
            }

            public bool RemoveRegion(Collider c) {
                var previousState = IsTrespassing;
                bool changed1 = _allowedRegions.Remove(c);
                bool changed2 = _trespassingRegions.Remove(c);
                if (changed1 || changed2) {
                    UpdateState(previousState);
                    return true;
                }
                return false;
            }

            public void RemoveNulls() {
                var previousState = IsTrespassing;
                bool changed2 = _allowedRegions.RemoveWhere(c => c == null) > 0;
                bool changed1 = _trespassingRegions.RemoveWhere(c => c == null) > 0;
                if (changed1 || changed2) {
                    UpdateState(previousState);
                }
            }
            
            public void Clear() {
                if (AnyRegistered) {
                    bool previousState = IsTrespassing;
                    _trespassingRegions.Clear();
                    _allowedRegions.Clear();
                    UpdateState(previousState);
                }
            }

            public void AddFlag(string flag) {
                if (flag.IsNullOrWhitespace()) {
                    return;
                }
                _enablingFlags.Add(flag);
            }

            public void RemoveFlag(string flag) {
                if (flag.IsNullOrWhitespace()) {
                    return;
                }
                _enablingFlags.Remove(flag);
            }

            public void OnFlagChanged(string flag) {
                if (!_enablingFlags.Contains(flag)) {
                    return;
                }
                
                //TODO: Enabling regions (currently disabled on start aren't even stored)
                
                s_updatedColliders.Clear();
                s_updatedColliders.AddRange(_allowedRegions.Where(r => !r.GetComponentInParent<CrimeRegion>().Enabled));
                s_updatedColliders.AddRange(_trespassingRegions.Where(r => !r.GetComponentInParent<CrimeRegion>().Enabled));
                foreach (var z in s_updatedColliders) {
                    RemoveRegion(z);
                    RemoveFlag(z.GetComponentInParent<CrimeRegion>().EnablingFlag);
                }
                s_updatedColliders.Clear();
            }
        }
    }
}