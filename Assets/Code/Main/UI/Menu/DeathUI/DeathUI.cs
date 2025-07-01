using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;

namespace Awaken.TG.Main.UI.Menu.DeathUI {
    public partial class DeathUI : Model, IUIStateSource {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden);

        bool GoToJail { get; }
        public SceneReference SceneToTeleport { get; }
        public string IndexTag { get; }

        public DeathUI() { }

        public DeathUI(CrimeOwnerTemplate jailerCrimeOwner) {
            GoToJail = true;
            SceneToTeleport = jailerCrimeOwner.Prison;
            IndexTag = "Jail";
        }
        
        public DeathUI(SceneReference sceneToTeleport, string indexTag) {
            SceneToTeleport = sceneToTeleport;
            IndexTag = indexTag;
        }
        
        protected override void OnInitialize() {
            World.SpawnView<VModalBlocker>(this);
            if (SceneToTeleport != null) {
                if (GoToJail) {
                    World.SpawnView<VJailUI>(this, true);
                } else {
                    World.SpawnView<VDeathTeleportUI>(this, true);
                }
            } else {
                World.SpawnView<VDeathUI>(this, true);
            }
        }

        public void Revive() {
            World.Only<HeroDeath>().Revive();
            Discard();
        }
    }
}