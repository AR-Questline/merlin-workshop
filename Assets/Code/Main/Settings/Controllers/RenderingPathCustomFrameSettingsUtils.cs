using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    public static class RenderingPathCustomFrameSettingsUtils {
        public static void Override(
            ref FrameSettingsOverrideMask overrideMask, ref FrameSettings frameSettings, FrameSettingsField field,
            bool state) {
            overrideMask.mask[(uint)field] = true;
            frameSettings.SetEnabled(field, state);
        }
    }
}
