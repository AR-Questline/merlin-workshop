using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Variable: Check")]
    public class CEditorVariable : EditorCondition, IStoryVariableRef {

        [ShowIf(nameof(NeedContext))]
        [Tags(TagsCategory.Context)] 
        public string[] context = Array.Empty<string>();

        [ShowIf(nameof(NeedContext))]
        [List(ListEditOption.Buttons)]
        public Context[] contexts = Array.Empty<Context>();

        [NodeEnum]
        public Comparison comparison;
        [Header("Compare")][Getter]
        public Variable variableA;
        [Header("To")][Getter]
        public Variable variableB;

        bool NeedContext => variableA.type == VariableType.Custom || variableB.type == VariableType.Custom;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CVariable {
                context = context,
                contexts = contexts,
                comparison = comparison,
                variableA = variableA,
                variableB = variableB
            };
        }
        
        public string Summary() {
            return $"{variableA.Label()} {comparison.ToString()} {variableB.Label()}";
        }

        IEnumerable<Variable> IStoryVariableRef.variables {
            get {
                yield return variableA;
                yield return variableB;
            }
        }

        Context[] IStoryVariableRef.optionalContext => contexts;
        public string extraInfo => comparison.ToString();
    }

    public partial class CVariable : StoryCondition {
        public string[] context = Array.Empty<string>();
        public Context[] contexts = Array.Empty<Context>();
        public Comparison comparison;
        public Variable variableA;
        public Variable variableB;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            float valueA = variableA.GetValue(story, context, contexts);
            float valueB = variableB.GetValue(story, context, contexts);
            return valueA.CompareTo(valueB) == (int) comparison;
        }
    }
}