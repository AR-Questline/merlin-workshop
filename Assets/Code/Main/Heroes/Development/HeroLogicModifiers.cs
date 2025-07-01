using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Heroes.Development {
    public class HeroLogicModifiers : IEventSource  {
        public string ID => nameof(HeroLogicModifiers);

        public HeroLogicModifiers() {
            _disableBowPullMovementPenalties = new LogicModifierData(this, Events.DisableBowPenaltiesToggled);
        }
        
        // === Events
        public static class Events {
            public static readonly Event<Hero, bool> DisableBowPenaltiesToggled = new(nameof(DisableBowPenaltiesToggled));
        }

        // === LogicModifiers
        LogicModifierData _disableBowPullMovementPenalties;
        public ref LogicModifierData DisableBowPullMovementPenalties => ref _disableBowPullMovementPenalties;
        
        // === Helpers
        public struct LogicModifierData {
            readonly HeroLogicModifiers _logicModifiers;
            readonly Event<Hero, bool> _eventToTrigger;
            int _currentValue;

            public bool Get => _currentValue > 0;

            public LogicModifierData(HeroLogicModifiers logicModifiers,Event<Hero, bool> eventToTrigger) {
                _logicModifiers = logicModifiers;
                _eventToTrigger = eventToTrigger;
                _currentValue = 0;
            }
        
            public void Set(bool enable) {
                bool wasActive = Get;
                if (enable) {
                    _currentValue++;
                    if (!wasActive) {
                        World.EventSystem.Trigger(_logicModifiers, _eventToTrigger, true);
                    }
                } else {
                    _currentValue--;
                    if (_currentValue <= 0 && wasActive) {
                        World.EventSystem.Trigger(_logicModifiers, _eventToTrigger, false);
                    }

                    if (_currentValue < 0) {
                        Log.Critical?.Error($"Logic modifier value was negative! {this}");
                        _currentValue = 0;
                    }
                }
            }
        
            public static implicit operator bool(LogicModifierData data) => data.Get;
        }
    }
}