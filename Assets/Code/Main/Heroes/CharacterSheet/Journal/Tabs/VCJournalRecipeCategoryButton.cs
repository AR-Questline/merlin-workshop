using Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs {
    public class VCJournalRecipeCategoryButton : ViewComponent<JournalRecipeUI> {
        [SerializeField, RichEnumExtends(typeof(JournalRecipeSubTabType))] 
        RichEnumReference tabType;
        [SerializeField] ButtonConfig buttonConfig;
        
        JournalRecipeSubTabType Type => tabType.EnumAs<JournalRecipeSubTabType>();        
        
        public void Focus() {
            World.Only<Focus>().Select(buttonConfig.button);
        }
        
        protected override void OnAttach() {
            buttonConfig.InitializeButton(Select, Type.Title);
        }

        void Select() {
            Target.SetRecipeType(Type);
        }
    }
}