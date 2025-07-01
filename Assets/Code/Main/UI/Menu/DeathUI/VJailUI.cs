using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.UI.Menu.DeathUI {
    [UsesPrefab("UI/VJailUI")]
    public class VJailUI : VDeathTeleportUI{
        public static class Events {
            public static readonly Event<Hero, bool> GoingToJail = new(nameof(GoingToJail));
        }

        protected override void MapChange() {
            World.EventSystem.Trigger(Hero.Current, Events.GoingToJail, true);
            Portal.MapChangeTo(Hero.Current, Target.SceneToTeleport, World.Services.Get<SceneService>().ActiveSceneRef, Target.IndexTag);
        }
    }
}
