using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.General.NewThings {
    public partial class NewThingsTracker : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.NewThingsTracker;
        public Domain Domain => Domain.Gameplay;

        // === State
        [Saved] HashSet<string> _seenThings = new();

        readonly List<IModelNewThing> _toRemove = new();
        List<INewThingContainer> _containers = new(10);
        bool _allowShowNewThings = true;

        public void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<IModelNewThing>(), this, OnNewThing);
        }

        void OnNewThing(Model model) {
            IModelNewThing newThing = (IModelNewThing) model;
            if (!WasSeen(newThing.NewThingId)) {
                RefreshContainers();
            }
        }
        
        static bool FilterInactiveTabs(INewThingContainer c) {
            if (c is VCCharacterSheetTabButton tab) {
                return tab.Target != null && tab.IsSelected;
            }

            return true;
        }

        // === Methods
        public bool WasSeen(string id) {
            if (!_allowShowNewThings || string.IsNullOrEmpty(id)) return true;
            return _seenThings.Contains(id);
        }

        /// <summary>
        /// This is an expensive operation. It happens only when there are Proxy Carriers, so hopefully only in UI.  
        /// </summary>
        public bool ProxyHasNewThings(INewThingContainer proxy) {
            return World.All<IModelNewThing>().Any(newThing => !WasSeen(newThing.NewThingId) && proxy.NewThingBelongsToMe(newThing));
        }

        public void MarkSeen(INewThingCarrier carrier) {
            MarkSeen(carrier.NewThingModel);
        }

        public void MarkSeen(IModelNewThing newThing) {
            if (newThing == null) {
                return;
            }

            MarkSeenInternal(newThing.NewThingId);
            if (newThing.DiscardAfterMarkedAsSeen) {
                newThing.Discard();
            }
        }
        
        void MarkSeenInternal(string id) {
            if (id == null) return;
            if (_seenThings.Add(id)) {
                RefreshContainers();
            }
        }

        public void MarkAllAsSeen() {
            foreach (var newThing in World.All<IModelNewThing>()) {
                if (_containers.Where(FilterInactiveTabs).Any(c => c.NewThingBelongsToMe(newThing))) {
                    MarkSeenInternal(newThing.NewThingId);
                    newThing.Trigger(IModelNewThing.Events.NewThingRefreshed, newThing);
                    if (newThing.DiscardAfterMarkedAsSeen) {
                        _toRemove.Add(newThing);
                    }
                }
            }
            
            for (int i = _toRemove.Count; --i >= 0;) {
                _toRemove[i].Discard();
            }
            
            _toRemove.Clear();
        }
        
        public bool HasAnyThingsToMarkAsSeen() {
            var containers = _containers.Where(FilterInactiveTabs).ToList();
            foreach (var newThing in World.All<IModelNewThing>()) {
                if (containers.Any(c => c.NewThingBelongsToMe(newThing) && !WasSeen(newThing.NewThingId))) {
                    return true;
                }
            }

            return false;
        }

        public void RegisterContainer(INewThingContainer newThingContainer) {
            _containers.Add(newThingContainer);
        }

        public void UnregisterContainer(INewThingContainer newThingContainer) {
            _containers.Remove(newThingContainer);
        }

        void RefreshContainers() {
            for (int i = _containers.Count - 1; i >= 0; i--) {
                INewThingContainer container = _containers[i];
                container.RefreshNewThingsContainer();
            }
        }

        // === Interfaces
        public bool RemoveOnDomainChange() {
            _seenThings.Clear();
            _containers.Clear();
            return false;
        }
    }
}