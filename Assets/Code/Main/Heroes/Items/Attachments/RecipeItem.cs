using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class RecipeItem : Element<Item>, IRefreshedByAttachment<RecipeItemAttachment> {
        public override ushort TypeForSerialization => SavedModels.RecipeItem;

        RecipeItemAttachment _spec;
        
        public void InitFromAttachment(RecipeItemAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeAttached, BeforeAttached, this);
        }

        void BeforeAttached(HookResult<IModel, RelationEventData> result) {
            if (result.Value.to is Hero hero) {
                hero.Element<HeroRecipes>().LearnRecipe(_spec.Recipe);
                if (_spec.DestroyOnPickup) {
                    ParentModel.Discard();
                    result.Prevent();
                }
            }
        }
    }
}