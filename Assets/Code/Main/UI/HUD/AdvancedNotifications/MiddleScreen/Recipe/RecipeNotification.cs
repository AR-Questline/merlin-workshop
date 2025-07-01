using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Recipe {
    public partial class RecipeNotification : Element<RecipeNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        readonly RecipeData _data;
        
        public string RecipeName { get; private set; }
        public string RecipeDescription { get; private set; }
        public SpriteReference IconReference => _data.recipe.Outcome.iconReference.Get();
        
        public RecipeNotification(RecipeData data) {
            _data = data;
        }

        protected override void OnInitialize() {
            var resultItem = World.Add(_data.recipe.Create(null, 0));
            RecipeName = resultItem.DisplayName;
            RecipeDescription = resultItem.BaseDescriptionFor(Hero.Current);
            resultItem.Discard();
        }
    }
}