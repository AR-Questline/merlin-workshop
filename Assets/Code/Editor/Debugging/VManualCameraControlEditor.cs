using Awaken.TG.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging
{
    [CustomEditor(typeof(VManualCameraControl))]
    public class VManualCameraControlEditor : UnityEditor.Editor {
        // === Constants

        const string CameraControlLabel =
            "\nDrag here to rotate camera\n" +
            "Drag with middle button to pan camera\n" +
            "Mousewheel to zoom\n" +
            "Alt+Mousewheel to roll\n" +
            "Hold Ctrl for more precision";

        // === GUI code

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            GUILayout.BeginVertical();
            GUI.backgroundColor = new Color(0.5f, 0.7f, 0.5f);
            GUILayout.Box(CameraControlLabel, GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(100));
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();

            VManualCameraControl pcc = (VManualCameraControl) target;

            // drags
            Event e = Event.current;
            float factor = 1f;
            if (e.command || e.control) {
                factor = 0.15f;
            }
            if (e.type == EventType.MouseDrag) {
                if (e.button == 0) {
                    pcc.PerformRotate(Vector3.up, GetResponseSize(e.delta.x) * factor);
                    pcc.PerformRotate(Vector3.right, GetResponseSize(e.delta.y) * factor);
                }
                if (e.button == 1 || e.button == 2) {
                    pcc.PerformTranslate(Vector3.right, GetResponseSize(e.delta.x) * factor);
                    pcc.PerformTranslate(Vector3.up, GetResponseSize(-e.delta.y) * factor);
                }
            }
            if (e.type == EventType.ScrollWheel) {
                if (e.alt) {
                    pcc.PerformRotate(Vector3.forward, GetResponseSize(e.delta.y * 2) * factor);
                }
                else {
                    pcc.PerformTranslate(Vector3.forward, GetResponseSize(-e.delta.y * 2) * factor);
                }
            }

            // buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUI.skin.button, GUILayout.ExpandWidth(false))) pcc.PerformReset();
            if (GUILayout.Button("Straighten", GUI.skin.button, GUILayout.ExpandWidth(false))) pcc.PerformStraighten();
            pcc.autoStraighten = GUILayout.Toggle(pcc.autoStraighten, "Horizon lock");
            GUILayout.EndHorizontal();
        }

        float GetResponseSize(float pixels) {
            return Mathf.Sign(pixels) * Mathf.Pow(Mathf.Abs(pixels / 5f), 1.5f);
        }
    }
}
