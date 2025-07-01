using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Crafting.Recipes {
    public interface IRuntimeRecipe : IRecipe {
        string ITemplate.GUID {
            get => string.Empty;
            set => throw new System.NotImplementedException();
        }
        TemplateMetadata ITemplate.Metadata => null;
    }
}