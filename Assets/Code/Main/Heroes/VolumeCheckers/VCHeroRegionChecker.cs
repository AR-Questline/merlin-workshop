using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VCHeroRegionChecker : VCHeroVolumeChecker {
        readonly Dictionary<SimpleCrimeRegion, HashSet<Collider>> _currentRegions = new();
        readonly EnumerableCache<SimpleCrimeRegion> _regionsCache = new(4);
        readonly EnumerableCache<Collider> _collidersCache = new(4);

        protected override void OnEnter(Collider other) {
            var factionOwnedRegion = other.GetComponentInParent<SimpleCrimeRegion>();
            if (factionOwnedRegion == null) {
                return;
            }

            if (_currentRegions.TryGetValue(factionOwnedRegion, out var colliders)) {
                colliders.Add(other);
            } else {
                _currentRegions.Add(factionOwnedRegion, new HashSet<Collider> { other });
                RegionChangedData regionChangedData = new(factionOwnedRegion, _currentRegions.Keys);
                Target.Trigger(Hero.Events.FactionRegionEntered, regionChangedData);
            }
        }

        protected override void OnExit(Collider other, bool destroyed = false) {
            if (destroyed) {
                CleanupRegions();
                return;
            }

            var factionOwnedRegion = other.GetComponentInParent<SimpleCrimeRegion>();
            if (factionOwnedRegion == null) {
                return;
            }

            if (!_currentRegions.TryGetValue(factionOwnedRegion, out var colliders)) {
                return;
            }

            colliders.Remove(other);
            if (colliders.Count != 0) {
                return;
            }

            _currentRegions.Remove(factionOwnedRegion);
            RegionChangedData regionChangedData = new(factionOwnedRegion, _currentRegions.Keys);
            Target.Trigger(Hero.Events.FactionRegionExited, regionChangedData);
        }

        void CleanupRegions() {
            foreach (var region in _regionsCache[_currentRegions.Keys]) {
                if (region == null) {
                    _currentRegions.Remove(region);
                    Target.Trigger(Hero.Events.FactionRegionExited, new RegionChangedData(region, _currentRegions.Keys));
                } else {
                    HashSet<Collider> regionColliders = _currentRegions[region];
                    CleanupColliders(regionColliders);
                    if (regionColliders.Count == 0) {
                        _currentRegions.Remove(region);
                        Target.Trigger(Hero.Events.FactionRegionExited, new RegionChangedData(region, _currentRegions.Keys));
                    }
                }
            }
        }

        void CleanupColliders(ICollection<Collider> colliders) {
            foreach (var col in _collidersCache[colliders]) {
                if (col == null) {
                    colliders.Remove(col);
                }
            }
        }

        protected override void OnFirstVolumeEnter(Collider other) { }
        protected override void OnAllVolumesExit() { }
        protected override void OnStay() { }
    }

    public struct RegionChangedData {
        public SimpleCrimeRegion CrimeRegion { [UnityEngine.Scripting.Preserve] get; }
        public IEnumerable<SimpleCrimeRegion> CurrentRegions { get; }

        public RegionChangedData(SimpleCrimeRegion crimeRegion, IEnumerable<SimpleCrimeRegion> currentRegions) {
            CrimeRegion = crimeRegion;
            CurrentRegions = currentRegions;
        }
    }
}