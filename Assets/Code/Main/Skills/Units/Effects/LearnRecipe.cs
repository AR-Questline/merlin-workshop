using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class LearnRecipe : ARUnit, ISkillUnit {
        protected override void Definition() {
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            var recipe = RequiredARValueInput<TemplateWrapper<IRecipe>>("recipe");

            DefineSimpleAction(flow => {
                if (character.Value(flow) is Hero hero) {
                    hero.Element<HeroRecipes>().LearnRecipe(recipe.Value(flow).Template);
                }
            });
        }
    }
}