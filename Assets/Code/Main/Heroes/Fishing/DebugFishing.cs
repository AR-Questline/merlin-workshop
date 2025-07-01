using System.Collections.Generic;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Fishing {
    public partial class DebugFishing : MonoBehaviour, IDomainBoundService {
        bool _enabled;
        List<IEventListener> _listeners = new();
        
        public Domain Domain => Domain.Gameplay;
        public bool IsEnabled => _enabled;

        public DebugFishing Init() {
            _listeners.Add(World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.OnFullSceneLoaded, OnSceneChanged));
            _listeners.Add(World.EventSystem.ListenTo(EventSelector.AnySource, CharacterFishingRod.Events.FightingFishAcquired, OnFightingFishAcquired));
            _listeners.Add(World.EventSystem.ListenTo(EventSelector.AnySource, CharacterFishingRod.Events.OnFishVolumesCulminated, OnFishVolumesCulminated));
            _listeners.Add(World.EventSystem.ListenTo(EventSelector.AnySource, CharacterFishingRod.Events.OnFishingBobberDestroyed, OnFishingBobberDestroyed));
            return this;
        }

        public void Enable() {
            SetHighDensities();
            _enabled = true;
        }

        public void Disable() {
            _enabled = false;
        }
        
        public bool RemoveOnDomainChange() {
            if (_listeners.Count > 0) {
                var listeners = _listeners.ToArray();
                for (int i = listeners.Length - 1; i >= 0; i--) {
                    World.EventSystem.DisposeListener(ref listeners[i]);
                }
            }
            
            _listeners.Clear();
            return true;
        }

        void OnFishVolumesCulminated(IEnumerable<IFishVolume> fishVolumes) {
            if (_enabled) {
                Hero.Current.Trigger(VCHeroDamageDealtStatusResistance.Events.OnDebugFishDataShow, fishVolumes);
            }
        }
        
        void OnFishingBobberDestroyed() {
            if (_enabled) {
                Hero.Current.Trigger(VCHeroDamageDealtStatusResistance.Events.OnDebugFishDataHide, Hero.Current);
            }
        }

        void OnFightingFishAcquired(CharacterFishingRod rod) {
            if (_enabled) {
                rod.DebugSetLowFishHp();
            }
        }

        void OnSceneChanged() {
            if (_enabled) {
                SetHighDensities();
            }
        }
        
        void SetHighDensities() {
            foreach (FishVolume volume in FindObjectsByType<FishVolume>(FindObjectsSortMode.None)) {
                volume.DebugSetHugeDensity();
            }
        }
    }
}