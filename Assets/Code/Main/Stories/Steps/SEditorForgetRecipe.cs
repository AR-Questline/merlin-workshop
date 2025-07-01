using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Forget Recipe")]
    public class SEditorForgetRecipe : EditorStep {
        [TemplateType(typeof(IRecipe))]
        public TemplateReference recipe;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SForgetRecipe {
                recipe = recipe
            };
        }
    }

    public partial class SForgetRecipe : StoryStep {
        public TemplateReference recipe;
        
        public override StepResult Execute(Story story) {
            story.Hero.Element<HeroRecipes>().ForgetRecipe(recipe.Get<IRecipe>());
            return StepResult.Immediate;
        }
    }
}