using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Utils {
    public static class VisualScriptingCanvasUtils {
        public static void RefreshDescriptor(this IState state) {
            DescriptorProvider.instance.TriggerDescriptionChange(state);
        }
        public static void RefreshDescriptor(this IUnit unit) {
            DescriptorProvider.instance.TriggerDescriptionChange(unit);
        }
    }
}