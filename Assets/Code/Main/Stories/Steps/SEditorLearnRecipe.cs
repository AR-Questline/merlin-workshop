using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Learn Recipe")]
    public class SEditorLearnRecipe : EditorStep {
        [TemplateType(typeof(IRecipe))]
        public TemplateReference recipe;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLearnRecipe {
                recipe = recipe
            };
        }
    }

    public partial class SLearnRecipe : StoryStep {
        public TemplateReference recipe;
        
        public override StepResult Execute(Story story) {
            story.Hero.Element<HeroRecipes>().LearnRecipe(recipe.Get<IRecipe>());
            return StepResult.Immediate;
        }
    }
}