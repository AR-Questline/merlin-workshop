using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.Heroes.Spyglass {
    public partial class SpyglassMask : Element<Item>, IUIStateSource {
        public sealed override bool IsNotSaved => true;

        static VolumeWrapper PostProcess => World.Services.Get<SpecialPostProcessService>().VolumeSpyglass;
        static float Speed => GameConstants.Get.SpyglassVolumeChangeSpeed;

        public UIState UIState => UIState.BaseState.WithHUDState(HUDState.Spyglass).WithHeroBars(false);

        protected override void OnInitialize() {
            PostProcess.SetWeight(1f, Speed);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            PostProcess.SetWeight(0f, Speed);
        }
    }
}