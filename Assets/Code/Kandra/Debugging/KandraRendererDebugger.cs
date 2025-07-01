using UnityEngine;

namespace Awaken.Kandra.Debugging {
    public partial class KandraRendererDebugger {
        const int Indent = 12;
        Font _font;
        bool _hasFont;
        bool _useMonoSpaced = true;

        GUIStyle BoldLabelStyle =>
#if UNITY_EDITOR
            UnityEditor.EditorStyles.boldLabel;
#else
            Awaken.Utility.UI.TGGUILayout.BoldLabel;
#endif

        public KandraRendererDebugger() {
            _font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
            _hasFont = _font != null;
        }

        public void OnGUI() {
            var instance = KandraRendererManager.Instance;
            if (instance == null) {
                GUILayout.Label("KandraRendererManager is null");
                return;
            }

            var oldFont = GUI.skin.font;
            if (_hasFont && !_font) {
                _font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
            }
            if (_useMonoSpaced && _font) {
                GUI.skin.font = _font;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kandra debug window", BoldLabelStyle);
            GUILayout.Space(32);
            _useMonoSpaced = GUILayout.Toggle(_useMonoSpaced, "Mono spaced");
            GUILayout.EndHorizontal();

            VfxDebug();
            IndicesDebug();
            BlendshapesDebug();
            MeshesDebug();
            RenderersDebug();

            GUI.skin.font = oldFont;
        }
    }
}
