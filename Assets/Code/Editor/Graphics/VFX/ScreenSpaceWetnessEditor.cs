using Awaken.TG.Graphics.VFX;
using UnityEditor.Rendering.HighDefinition;

namespace Awaken.TG.Editor.Graphics.VFX {
    [CustomPassDrawer(typeof(ScreenSpaceWetness))]
    public class ScreenSpaceWetnessEditor : CustomPassDrawer {
        protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
    }
}