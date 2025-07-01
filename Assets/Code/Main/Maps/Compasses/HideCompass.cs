using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Maps.Compasses {
    public partial class HideCompass : Model {
        public override ushort TypeForSerialization => SavedModels.HideCompass;

        [Saved] public string SourceID { get; private set; }
        public override Domain DefaultDomain => Domain.Gameplay;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        HideCompass() {}

        public HideCompass(string sourceID) {
            SourceID = sourceID;
        }
        
        protected override void OnInitialize() {
            UIStateStack.Instance.PushState(UIState.TransparentState.WithHUDState(HUDState.CompassHidden), this);
        }
    }
}