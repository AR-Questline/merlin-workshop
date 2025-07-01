using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.UI.PhotoMode {
    [UsesPrefab("UI/" + nameof(VPhotoModeUI))]
    public class VPhotoModeUI : View<PhotoModeUI>, IAutoFocusBase, IFocusSource {
        [SerializeField] Transform uiParent;
        [SerializeField] ARButton photoModeDefaultButton;
        [SerializeField] Transform promptHost;
        
        public Transform PromptsHost => promptHost;
        public bool ForceFocus => true;
        public Component DefaultFocus => photoModeDefaultButton;

        protected override void OnInitialize() {
            Target.ListenTo(PhotoModeUI.Events.UIToggled, OnUIToggled, this);
        }

        void OnUIToggled(bool enabled) {
            uiParent.TrySetActiveOptimized(enabled);
        }
    }
}