using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class TeleportHeroOnHeroDeath : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.TeleportHeroOnHeroDeath;

        [Saved] SceneReference _targetScene;
        [Saved] string _sceneIndex;
        [Saved] StoryBookmark _bookmark;
        [Saved] bool _discardOnSceneChange;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        TeleportHeroOnHeroDeath() {}
        
        public TeleportHeroOnHeroDeath(SceneReference sceneReference, string sceneIndex, StoryBookmark bookmark, bool discardOnSceneChange) {
            _targetScene = sceneReference;
            _sceneIndex = sceneIndex;
            _bookmark = bookmark;
            _discardOnSceneChange = discardOnSceneChange;
        }

        protected override void OnInitialize() {
            if (_discardOnSceneChange) {
                ParentModel.ListenTo(Hero.Events.HeroLongTeleported, Discard, this);
            }
        }

        public void HeroKilled() {
            if (_bookmark is { IsValid: true }) {
                Story.StartStory(StoryConfig.Base(_bookmark, typeof(VDialogue)));
            }
            World.Add(new DeathUI(_targetScene, _sceneIndex));
            Discard();
        }
    }
}