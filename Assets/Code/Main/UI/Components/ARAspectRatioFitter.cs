using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class ARAspectRatioFitter : AspectRatioFitter {
        protected override void OnEnable() {
            base.OnEnable();
            SetDirty();
        }
    }
}