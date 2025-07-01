using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.RadialMenu;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    [SpawnsView(typeof(VQuickUseWheelUI))]
    public partial class QuickUseWheelUI : Model, IRadialMenuUI {
        public sealed override bool IsNotSaved => true;

        public override Domain DefaultDomain => Domain.Gameplay;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.QuestTrackerHidden).WithHeroBars(true);
        
        [UnityEngine.Scripting.Preserve] public Prompts Prompts => Element<Prompts>();
        public KeyBindings MainKey => KeyBindings.UI.HUD.QuickUseWheel;
        
        public Hero Hero => Hero.Current;
        public HeroItems HeroItems => Hero.HeroItems;
        public ARDateTime WeatherTime => World.Only<GameRealTime>().WeatherTime;

        public static void Show() {
            ModelUtils.GetSingletonModel<QuickUseWheelUI>();
        }

        protected override void OnInitialize() {
            Hero.ListenTo(Stat.Events.StatChanged(AliveStatType.Health), CheckHeroDied, this);
        }

        void CheckHeroDied() {
            if (Hero.Current.Health.ModifiedValue <= 0) {
                Discard();
            }
        }
    }
}