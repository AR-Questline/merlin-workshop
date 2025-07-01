using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Settings.GammaSettingScreen {
    public partial class GammaScreen : Model, IClosable {
        public const float SliderStepChange = 0.1f;
        public const int MinValue = 0;
        public const int MaxValue = 2;
        
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        float CachedValue { get; }
        bool Closable { get; }
        float GammaValue => GammaSetting.Value;
        GammaSetting GammaSetting => World.Only<GammaSetting>();

        GammaScreen(bool closable) {
            Closable = closable;
            CachedValue = GammaValue;
        }
        
        protected override void OnInitialize() {
            World.SpawnView<VModalBlocker>(this);
            var view = World.SpawnView<VGammaScreen>(this, true);
            var slider = World.SpawnView<VGammaSlider>(this, forcedParent: view.SliderParent);

            slider.Setup(MinValue, MaxValue, false, GammaValue, SliderStepChange, SetGamma);
        }

        public void Close() {
            if (Closable) {
                SetGamma(CachedValue);
                Discard();
            }
        }

        void SetGamma(float value) {
            var settings = GammaSetting;
            settings.Value = value;
            settings.Apply(out _);
        }

        public static async UniTask ShowGammaScreen(bool closeable = true) {
            var gammaScreen = new GammaScreen(closeable);
            World.Add(gammaScreen);

            await AsyncUtil.WaitForDiscard(gammaScreen);
        }
    }
}