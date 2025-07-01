using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.AutoGuards {
    public partial class DiscardLocationWhenNotInVisualBand : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.DiscardLocationWhenNotInVisualBand;

        protected override void OnInitialize() {
            ParentModel.ListenTo(NpcElement.Events.AfterNpcOutOfVisualBand, () => DiscardAfterFrame().Forget(), this);
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.BeforeDroppedPreviousDomain, ParentModel, _ => ParentModel.Discard());
        }

        async UniTaskVoid DiscardAfterFrame() {
            if (await AsyncUtil.DelayFrame(this)) {
                ParentModel.Discard();
            }
        }
    }
}