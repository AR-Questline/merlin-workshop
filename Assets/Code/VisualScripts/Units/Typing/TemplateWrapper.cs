using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Typing {
    public class TemplateWrapper<T> where T : class, ITemplate {
        public T Template { get; }

        public TemplateWrapper(T template) {
            Template = template;
        }

        public bool Equals(T template) {
            return Template == template;
        }

        [UnityEngine.Scripting.Preserve]
        public bool Equals(TemplateWrapper<T> other) {
            return Template == other?.Template;
        }
        
        public override bool Equals(object obj) {
            return obj switch {
                TemplateWrapper<T> wrapper => Equals(wrapper.Template),
                T template => Equals(template),
                _ => false
            };
        }

        public override int GetHashCode() {
            return Template.GetHashCode();
        }

        [UnitCategory("AR/Templates/Literals")]
        [TypeIcon(typeof(FlowGraph))]
        public abstract class Literal : ARUnit {
            protected abstract TemplateReference Reference { get; }
            
            protected override void Definition() {
                Equals(null, null);
                ValueOutput(typeof(T).Name, _ => new TemplateWrapper<T>(Reference.Get<T>()));
                ValueOutput($"{typeof(T).Name}_Template", _ => Reference.Get<T>());
            }
        }

        [UnitCategory("AR/Templates/Variables")]
        [TypeIcon(typeof(FlowGraph))]
        public abstract class Variable : ARUnit {

            [Serialize, Inspectable, UnitHeaderInspectable]
            public VariableKind kind;

            protected override void Definition() {
                var name = InlineARValueInput("", string.Empty);
                var go = kind == VariableKind.Object ? FallbackARValueInput("object", flow => flow.stack.self) : null;
                ValueOutput("", flow => {
                    var variables = kind switch {
                        VariableKind.Flow => flow.variables,
                        VariableKind.Graph => Variables.Graph(flow.stack),
                        VariableKind.Object => Variables.Object(go.Value(flow)),
                        VariableKind.Scene => Variables.Scene(flow.stack.scene),
                        VariableKind.Application => Variables.Application,
                        VariableKind.Saved => Variables.Saved,
                        _ => throw new UnexpectedEnumValueException<VariableKind>(kind)
                    };
                    var variable = variables.Get(name.Value(flow));

                    if (variable is TemplateReference reference) {
                        return new TemplateWrapper<T>(reference.Get<T>());
                    } else {
                        throw new Exception("Variable must be template reference");
                    }
                });
            }
        }
    }

    [UnityEngine.Scripting.Preserve]
    public class StatusTemplateLiteral : TemplateWrapper<StatusTemplate>.Literal {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(StatusTemplate))]
        public TemplateReference reference;
        protected override TemplateReference Reference => reference;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class SkillGraphLiteral : TemplateWrapper<SkillGraph>.Literal {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(SkillGraph))]
        public TemplateReference reference;
        protected override TemplateReference Reference => reference;
    }

    [UnityEngine.Scripting.Preserve]
    public class RecipeLiteral : TemplateWrapper<IRecipe>.Literal {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(IRecipe))]
        public TemplateReference reference;
        protected override TemplateReference Reference => reference;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class LocationTemplateLiteral : TemplateWrapper<LocationTemplate>.Literal {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(LocationTemplate))]
        public TemplateReference reference;
        protected override TemplateReference Reference => reference;
    }

    [UnityEngine.Scripting.Preserve]
    public class LocationTemplateVariable : TemplateWrapper<LocationTemplate>.Variable { }
    
    [UnityEngine.Scripting.Preserve]
    public class ItemTemplateLiteral : TemplateWrapper<ItemTemplate>.Literal {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(ItemTemplate))]
        public TemplateReference reference;
        protected override TemplateReference Reference => reference;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class ItemTemplateVariable : TemplateWrapper<ItemTemplate>.Variable { }
    
    [UnityEngine.Scripting.Preserve]
    public class NpcTemplateLiteral : TemplateWrapper<NpcTemplate>.Literal {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(NpcTemplate))]
        public TemplateReference reference;
        protected override TemplateReference Reference => reference;
    }

    [UnityEngine.Scripting.Preserve]
    public class FactionTemplateLiteral : TemplateWrapper<FactionTemplate>.Literal {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(FactionTemplate))]
        public TemplateReference reference;
        protected override TemplateReference Reference => reference;
    }
}