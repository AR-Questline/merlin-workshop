using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    /// <summary>
    /// This is an armed version of a variable. It contains contexts, api and all other necessary data to do anything 
    /// </summary>
    public class VariableHandle {
        readonly string _name;
        readonly float _value;
        readonly VariableType _type;

        readonly float? _debugOverride;
        readonly ContextualFacts _facts;

        public VariableHandle([CanBeNull] Story story, Variable variable, string[] context, Context[] contexts) {
            _name = variable.name;
            _value = variable.value;
            _type = variable.type;
            _debugOverride = variable.debugOverride;
            _facts = World.Services.Get<GameplayMemory>().Context(StoryUtils.Context(story, context, contexts));
        }
        
        public bool HasValue() => _facts.HasValue(_name);
        
        public void SetValue() => SetValue(ValueForSet());
        public void SetValue(float newValue) {
            _facts.Set(_name, newValue);
        }

        public void AddValue() {
            float currentValue = _facts.Get(_name, 0f);
            float newValue = currentValue + ValueForSet();
            _facts.Set(_name, newValue);
        }

        public void MultiplyValue() {
            float currentValue = _facts.Get(_name, 1f);
            float newValue = currentValue * ValueForSet();
            _facts.Set(_name, newValue);
        }

        public float GetValue(float defaultValue = 0f) {
            return ValueForGet(defaultValue);
        }
        
        // === Helper
        float ValueForSet() {
            return _type switch {
                VariableType.Custom => _value,
                VariableType.Const => _value,
                VariableType.CurrentDay => World.Only<GameRealTime>().WeatherTime.Day + _value,
                VariableType.CurrentWeek => World.Only<GameRealTime>().WeatherTime.Week + (int)_value,
                VariableType.Defined => throw new InvalidOperationException("VariableHandle should never be of type Defined"),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        float ValueForGet(float defaultValue) {
            if (_debugOverride != null) {
                return _debugOverride.Value;
            }
            
            return _type switch {
                VariableType.Custom => GetCustomValue(),
                VariableType.Const => _value,
                VariableType.CurrentDay => World.Only<GameRealTime>().WeatherTime.Day,
                VariableType.CurrentWeek => World.Only<GameRealTime>().WeatherTime.Week,
                VariableType.Defined => throw new InvalidOperationException("VariableHandle should never be of type Defined"),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            float GetCustomValue() {
                float memVal = _facts.Get(_name, float.MinValue);
                if (memVal == float.MinValue) {
                    _facts.Set(_name, defaultValue);
                    return defaultValue;
                } else {
                    return memVal;
                }
            }
        }
    }
}