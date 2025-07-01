using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroDeath : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroDeath;

        const float TimeToDisplayDeathScreen = 1f;
        
        protected override void OnInitialize() {
            ShowDeathUI().Forget();
        }

        async UniTaskVoid ShowDeathUI() {
            if (await AsyncUtil.DelayTime(this, TimeToDisplayDeathScreen, true)) {
                World.Add(new DeathUI());
            }
        }

        public void Revive() {
            Services.TryGet<AudioCore>()?.Play();
            ParentModel.Revive();
            Discard();
        }
    }
}