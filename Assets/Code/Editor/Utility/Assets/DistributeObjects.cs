using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Assets {
    public class DistributeObjects : EditorWindow {
        bool _groupEnabled;
        Mode _mode;
        float _offset;

        void OnGUI() {
            GUILayout.Label("Base Settings", style: EditorStyles.boldLabel);
            _offset = EditorGUILayout.FloatField(value: _offset);
            GUILayout.Space(25);

            _groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", toggle: _groupEnabled);

            if (EditorGUILayout.DropdownButton(new GUIContent("Axis"), focusType: FocusType.Keyboard)) {
                GenericMenu menu = new();
                AddMenuItemForColor(menu: menu, "X", mode: Mode.X);
                AddMenuItemForColor(menu: menu, "Y", mode: Mode.Y);
                AddMenuItemForColor(menu: menu, "Z", mode: Mode.Z);
                menu.ShowAsContext();
            }

            EditorGUILayout.EndToggleGroup();
            if (GUILayout.Button("Distribute!")) Distribute();
        }

        void AddMenuItemForColor(GenericMenu menu, string menuPath, Mode mode) {
            menu.AddItem(new GUIContent(text: menuPath), _mode.Equals(obj: mode), func: OnModeSelected, userData: mode);
        }

        void OnModeSelected(object mode) {
            _mode = (Mode)mode;
        }

        [MenuItem("TG/Assets/Distribute Objects")]
        static void Init() {
            DistributeObjects window = (DistributeObjects)GetWindow(typeof(DistributeObjects));
            window.Show();
        }

        void Distribute() {
            GameObject[] objs = Selection.gameObjects;
            float offsetSoFar = 0f;
            foreach (GameObject go in objs) {
                Transform t = go.transform;
                MeshFilter meshFilter = go.GetComponentInChildren<MeshFilter>();
                Vector3 size = meshFilter ? meshFilter.sharedMesh.bounds.size : Vector3.one;
                float val = 0f;
                switch (_mode) {
                    default:
                    case Mode.X:
                        val = size.x + _offset + offsetSoFar;
                        t.position = new Vector3(x: val, 0, 0);
                        offsetSoFar += _offset + size.x;
                        break;
                    case Mode.Y:
                        val = size.y + _offset + offsetSoFar;
                        t.position = new Vector3(0, y: val, 0);
                        offsetSoFar += _offset + size.y;
                        break;
                    case Mode.Z:
                        val = size.z + _offset + offsetSoFar;
                        t.position = new Vector3(0, 0, z: val);
                        offsetSoFar += _offset + size.z;
                        break;
                }
            }
        }

        enum Mode {
            X,
            Y,
            Z
        }
    }
}