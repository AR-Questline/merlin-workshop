using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

namespace Awaken.TG.Main.Crafting.AlchemyCrafting {
    public class AlchemyTemplate : CraftingTemplate {
        public override IEnumerable<IRecipe> Recipes => recipes.Select(r => r.Get<IRecipe>());

        [TemplateType(typeof(AlchemyRecipe))]
        public TemplateReference[] recipes = Array.Empty<TemplateReference>();
        
        // === Editor only
#if UNITY_EDITOR
        
        [Button, Title("Editor Tools", null, TitleAlignments.Centered)]
        void SortRecipesByOutcome() {
            //recipes.Sort(Comparison);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// This list is to allow for adding multiple Templates at once. as TemplateReference currently doesn't support this.
        /// </summary>
        [ShowInInspector, HideInPlayMode, OnValueChanged(nameof(CustomAddFunction)), PropertyOrder(1), HideReferenceObjectPicker, ListDrawerSettings(HideAddButton = true)]
        List<AlchemyRecipe> _dragRecipesHere = new();

        void CustomAddFunction() {
            HashSet<TemplateReference> temp = new(_dragRecipesHere.Select(r => new TemplateReference(GetAssetGUID(r))));
            //temp.AddRange(recipes);
            recipes = temp.ToArray();
            _dragRecipesHere.Clear();
            SortRecipesByOutcome();
        }
#endif
    }
}