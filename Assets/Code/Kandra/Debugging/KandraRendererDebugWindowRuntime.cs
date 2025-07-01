using Awaken.Utility.Debugging;
using Awaken.Utility.UI;

namespace Awaken.Kandra.Debugging {
    public class KandraRendererDebugWindowRuntime : UGUIWindowDisplay<KandraRendererDebugWindowRuntime> {
        KandraRendererDebugger _debugger;

        protected override bool WithSearch => false;
        protected override bool WithScroll => false;

        protected override void Initialize() {
            base.Initialize();
            _debugger = new KandraRendererDebugger();
        }

        protected override void DrawWindow() {
            _debugger.OnGUI();
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowKandraRendererDebugWindow() {
            KandraRendererDebugWindowRuntime.Toggle(new UGUIWindowUtils.WindowPositioning(UGUIWindowUtils.WindowPosition.BottomLeft, 0.5f, 0.6f));
        }

        static bool IsDebugWindowShown() => KandraRendererDebugWindowRuntime.IsShown;
    }
}