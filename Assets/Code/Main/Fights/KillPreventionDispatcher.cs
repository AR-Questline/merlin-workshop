using System;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Fights {
    public partial class KillPreventionDispatcher : Element<IAlive> {
        public sealed override bool IsNotSaved => true;

        readonly List<KillPreventionListener> _listeners = new ();

        bool IsEmpty => _listeners.Count == 0;
        
        public new static class Events {
            public static readonly Event<IAlive, KillPreventedData> KillPrevented = new(nameof(KillPrevented));
        }
        
        public static void RegisterListener(IAlive alive, IKillPreventionListener listener) {
            GetDispatcher(alive).RegisterListener(listener);
        } 
        
        public static void UnregisterListener(IAlive alive, IKillPreventionListener listener) {
            TryGetDispatcher(alive)?.UnregisterListener(listener);
        }
        
        public static bool HasActivePrevention(IAlive alive) {
            return TryGetDispatcher(alive) is {IsEmpty: false};
        }

        static KillPreventionDispatcher GetDispatcher(IAlive alive) {
            return alive.TryGetElement<KillPreventionDispatcher>() ?? alive.AddElement(new KillPreventionDispatcher());
        }
        
        static KillPreventionDispatcher TryGetDispatcher(IAlive alive) {
            return alive.TryGetElement<KillPreventionDispatcher>();
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(AfterParentModelFullyInitialized);
        }
        
        void AfterParentModelFullyInitialized() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.KillPreventionBeforeTakenFinalDamage, OnBeforeTakenFinalDamage, this);
        }

        void RegisterListener(IKillPreventionListener listener) {
            var newListener = new KillPreventionListener {
                listener = listener,
                priority = GetPriority(listener)
            };
            int index = _listeners.BinarySearch(newListener);
            if (index < 0) index = ~index;
            _listeners.Insert(index, newListener);
        }
        
        void UnregisterListener(IKillPreventionListener listener) {
            _listeners.RemoveAll(l => l.listener == listener);
            if (IsEmpty) {
                Discard();
            }
        }
        
        void OnBeforeTakenFinalDamage(HookResult<HealthElement, Damage> hook) {
            var parentModelCache = ParentModel;
            foreach (var listener in _listeners) {
                if (listener.listener.OnBeforeTakingFinalDamage(hook.Model, hook.Value)) {
                    parentModelCache.Trigger(Events.KillPrevented, new KillPreventedData(listener.listener, hook.Value));
                    hook.Prevent();
                    return;
                }
            }
        }

        static int GetPriority(IKillPreventionListener listener) {
            return listener switch {
                DuelistElement => 3,
                TemporaryDeathElement => 2,
                KillPreventionElement => 1,
                _ => 0
            };
        }
        
        struct KillPreventionListener : IComparable<KillPreventionListener> {
            public IKillPreventionListener listener;
            public int priority;
            
            public int CompareTo(KillPreventionListener other) {
                return other.priority.CompareTo(priority);
            }
        }
    }
    
    public interface IKillPreventionListener {
        bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage);
    }

    public struct KillPreventedData {
        [UnityEngine.Scripting.Preserve] public IKillPreventionListener killPreventor;
        [UnityEngine.Scripting.Preserve] public Damage preventedDamage;
        
        public KillPreventedData(IKillPreventionListener killPreventor, Damage preventedDamage) {
            this.killPreventor = killPreventor;
            this.preventedDamage = preventedDamage;
        }
    }
}
