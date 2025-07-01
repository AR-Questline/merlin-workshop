using System;
using System.Collections.Generic;
using System.Reflection;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units {
    [UnityEngine.Scripting.RequireDerived]
    public abstract class ARUnit : Unit {

        static ARUnit() {
            var supportedDefaultValuesTypes = typeof(ValueInput).GetField("typesWithDefaultValues", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as HashSet<Type>;
            if (supportedDefaultValuesTypes != null) {
                supportedDefaultValuesTypes.Add(typeof(List<GameObject>));
            } else {
                Log.Important?.Error("Adding custom default types for VS failed");
            }
        }
        
        protected T Model<T>(Flow flow) {
            return Variables.Object(flow.stack.self).Get<T>(VGUtils.ModelVariableName);
        }

        protected NpcElement NpcElement(Flow flow) {
            return Model<IModel>(flow).Element<NpcElement>();
        }

        protected NpcAI NpcAI(Flow flow) {
            return NpcElement(flow).NpcAI;
        }

        [UnityEngine.Scripting.Preserve]
        protected ICharacter Character(Flow flow) {
            return flow.stack.gameObject.GetComponentInParent<ICharacterView>().Character;
        }
        
        // === Defining

        protected (ControlInput enter, ControlOutput exit) DefineSimpleAction(string input, string output, Action<Flow> action) {
            var exit = ControlOutput(output);
            var enter = ControlInput(input, flow => {
                action(flow);
                return exit;
            });
            Succession(enter, exit);
            return (enter, exit);
        }
        protected (ControlInput enter, ControlOutput exit) DefineSimpleAction(Action<Flow> action) {
            return DefineSimpleAction("enter", "exit", action);
        }
        protected (ControlInput enter, ControlOutput exit) DefineNoNameAction(Action<Flow> action) {
            return DefineSimpleAction("", "", action);
        }

        protected InlineValueInput<T> InlineARValueInput<T>(string key, T @default) {
            return new(ValueInput(key, @default));
        }
        
        protected FallbackValueInput<T> FallbackARValueInput<T>(string key, Func<Flow, T> fallback) {
            return new(ValueInput<T>(key), fallback);
        }

        protected OptionalValueInput<T> OptionalARValueInput<T>(string key) {
            return new(ValueInput<T>(key));
        }

        protected RequiredValueInput<T> RequiredARValueInput<T>(string key) {
            return new(ValueInput<T>(key));
        }
        
        [UnityEngine.Scripting.Preserve]
        protected RequiredValueInput<T> RequiredARValueInput<T>(string key, ValueOutput output) {
            var value = ValueInput<T>(key);
            Requirement(value, output);
            return new RequiredValueInput<T>(value);
        }
        
        [UnityEngine.Scripting.Preserve]
        protected RequiredValueInput<T> RequiredARValueInput<T>(string key, ControlInput input) {
            var value = ValueInput<T>(key);
            Requirement(value, input);
            return new RequiredValueInput<T>(value);
        }
    }
}
