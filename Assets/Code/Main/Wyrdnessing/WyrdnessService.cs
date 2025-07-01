using System.Collections.Generic;
using Awaken.TG.Graphics.DayNightSystem;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Wyrdnessing {
    public class WyrdnessService : IService {
        static readonly UniversalProfilerMarker FullUpdateMarker = new("WyrdnessService.FullUpdate");
        static readonly UniversalProfilerMarker MovedMarker = new("WyrdnessService.Moved");
        
        readonly HashSet<IWyrdnessReactor> _listeners = new();
        
        readonly HashSet<IWyrdnightRepellerSource> _fastRepellers = new();
        readonly HashSet<IWyrdnightRepellerSource> _repellers = new();
        
        /// <summary>
        /// Defense against multiple evokations during scene changes
        /// </summary>
        bool _isRepellerUpdatingEnabled;
        SceneService _sceneService;
        
        /// <summary>
        /// Use this method if you want to ignore <see cref="WyrdnightDisabled"/> properties and only check if the position is in any repeller.
        /// </summary>
        public bool IsInRepeller(Vector3 position) => IsPositionInAnyRepeller(position, false);
        public bool WyrdnightDisabled => !_sceneService.IsOpenWorld || !Hero.Current.HeroWyrdNight.Night;
        public bool IsWyrdNight => _sceneService.IsOpenWorld && Hero.Current.HeroWyrdNight.Night;
        
        public void Init(SceneService sceneService) {
            _sceneService = sceneService;

            World.EventSystem.PreAllocateMyListeners(this, 464);

            World.EventSystem.ListenTo(EventSelector.AnySource, HeroWyrdNight.Events.WyrdNightChanged, this, OnNightChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<IWyrdnessReactor>(), this, AutoRegister);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<IWyrdnessReactor>(), this, AutoUnregister);
            World.EventSystem.ListenTo(SceneLifetimeEvents.Get.ID, SceneLifetimeEvents.Events.BeforeSceneValid, this, DisableRepellerUpdating);
            World.EventSystem.ListenTo(SceneLifetimeEvents.Get.ID, SceneLifetimeEvents.Events.AfterSceneFullyInitialized, this, EnableRepellerUpdating);
            World.EventSystem.ListenTo(SceneLifetimeEvents.Get.ID, SceneLifetimeEvents.Events.AfterSceneDiscarded, this, RecalculateRepellersIfNeeded);
        }

        void RecalculateRepellersIfNeeded(SceneLifetimeEventData obj) {
            if (!obj.IsMainScene) {
                OnRepellersChanged();
            }
        }

        void EnableRepellerUpdating(SceneLifetimeEventData _) {
            _isRepellerUpdatingEnabled = true;
            OnRepellersChanged();
        }

        void DisableRepellerUpdating(SceneLifetimeEventData _) {
            _isRepellerUpdatingEnabled = false;
        }
        

        // === Registration
        public void RegisterRepeller(IWyrdnightRepellerSource repeller) {
            if (repeller.IsFast && _fastRepellers.Add(repeller)) {
                OnRepellersChanged();
            } else if (!repeller.IsFast && _repellers.Add(repeller)) {
                OnRepellersChanged();
            }
        }

        public void UnregisterRepeller(IWyrdnightRepellerSource repeller) {
            // Handle case of save drop
            if (Hero.Current == null) {
                _fastRepellers.Clear();
                _repellers.Clear();
                return;
            }

            if (repeller.IsFast && _fastRepellers.Remove(repeller)) {
                OnRepellersChanged();
            } else if (!repeller.IsFast && _repellers.Remove(repeller)) {
                OnRepellersChanged();
            }
        }

        void AutoRegister(IModel model) {
            if (model is IWyrdnessReactor wyrdnessListener) {
                _listeners.Add(wyrdnessListener);
                wyrdnessListener.ListenTo(GroundedEvents.AfterMoved, OnListenerPositionChanged, this);
                
                OnListenerPositionChanged(wyrdnessListener);
            }
        }

        void AutoUnregister(IModel model) {
            if (model is IWyrdnessReactor wyrdnessListener) {
                if (!_listeners.Remove(wyrdnessListener)) {
                    Log.Debug?.Warning("Wyrdness listener was not on list but was unregistered");
                }
            }
        }
        
        // === Wyrdness checking
        
        bool IsPositionInAnyRepeller(Vector3 position, bool checkWyrdnightDisabled = true) {
            if (checkWyrdnightDisabled && WyrdnightDisabled) {
                return true;
            }
            
            foreach (var repeller in _fastRepellers) {
                if (repeller.IsPositionInRepeller(position)) {
                    return true;
                }
            }
            
            foreach (var repeller in _repellers) {
                if (repeller.IsPositionInRepeller(position)) {
                    return true;
                }
            }

            return false;
        }
        
        // === Event callbacks
        void OnRepellersChanged() {
            if (!_isRepellerUpdatingEnabled) return;
            
            FullUpdateMarker.Begin();
            if (WyrdnightDisabled) {
                foreach (var listener in _listeners) {
                    InformOfRepellerStateChanged(listener, true);
                }
            } else {
                foreach (var listener in _listeners) {
                    InformOfRepellerStateChanged(listener, IsPositionInAnyRepeller(listener.Coords));
                }
            }
            FullUpdateMarker.End();
        }
        
        void OnNightChanged(bool isNight) {
            OnRepellersChanged();
        }
        
        void OnListenerPositionChanged(IGrounded listener) {
            if (!_isRepellerUpdatingEnabled) return;
            
            MovedMarker.Begin();
            InformOfRepellerStateChanged((IWyrdnessReactor) listener, IsPositionInAnyRepeller(listener.Coords));
            MovedMarker.End();
        }
        
        // === Helpers
        static void InformOfRepellerStateChanged(IWyrdnessReactor listener, bool isInRepeller) {
            if (listener.IsSafeFromWyrdness == isInRepeller) return;
            
            listener.IsSafeFromWyrdness = isInRepeller;
            if (listener is Hero hero) {
                hero.HeroWyrdNight.OnWyrdNightRepellerChanged();
            }
        }
    }
}