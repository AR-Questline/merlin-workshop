using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Awaken.TG.Main.Stories.Api;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    /// <summary>
    /// Serializable variable used in StoryGraph
    /// </summary>
    [Serializable]
    public class Variable {

        // === Serialized fields
        public string name;
        public float value;
        public VariableType type = VariableType.Defined;
        
        [NonSerialized]
        public float? debugOverride;

        // === Operations
        public VariableHandle Prepare([CanBeNull] Story story, string[] context = null, Context[] contexts = null) {
            if (type == VariableType.Defined) {
                return DefinedVariable(story);
            } else {
                return new VariableHandle(story, this, context, contexts ?? Array.Empty<Context>());
            }
        }

        public float GetValue([CanBeNull] Story story, string[] context = null, Context[] contexts = null, float defValue = 0f) => 
            Prepare(story, context, contexts).GetValue(defValue);

        public string Label() {
            return type switch {
                VariableType.Custom => name,
                VariableType.Defined => name,
                VariableType.Const => value.ToString(CultureInfo.InvariantCulture),
                _ => type.ToString()
            };
        }
        
        VariableHandle DefinedVariable(Story story) {
            if (story == null) {
                return null;
            }
            return story.Variables.FirstOrDefault(v => v.name == name)?.Create(story, value);
        }
    }

    // Note: Should be converted to RichEnum on next growth
    public enum VariableType {
        Custom = 0,
        CurrentDay = 1,
        Const = 2,
        CurrentWeek = 3,
        Defined = 4,
    }

    // Attributes assigned to Variable, to recognize and properly draw setter and getter contexts.
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class SetterAttribute : Attribute {}
    
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class GetterAttribute : Attribute {}
    
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class ForceTypeAttribute : Attribute {
        public VariableType[] Types { get; }

        public ForceTypeAttribute(params VariableType[] types) {
            Types = types;
        }
    }
}